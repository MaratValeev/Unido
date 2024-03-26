using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using UnityEngine;

namespace Unido
{
    /// <include file='Documentation.xml' path='docs/members[@name="DownloadService"]/*' />
    public partial class DownloadService : IDisposable
    {
        private List<DownloadProcess> currentDownloads;
        private HttpClient client;

        public ILogger Logger { get; set; }
        public DownloadOptions DefaultDownloadOptions { get; private set; }
        public IReadOnlyCollection<DownloadProcess> CurrentDownloadProcesses => currentDownloads.AsReadOnly();

        public DownloadService(DownloadServiceConfig config)
        {
            if (config == null)
            {
                config = new DownloadServiceConfig();
            }

            currentDownloads = new List<DownloadProcess>();
            client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(config.Timeout)
            };

            Logger = config.Logger;
            DefaultDownloadOptions = new DownloadOptions();
            Logger?.Log($"Initialize {nameof(DownloadService)} completed");
        }

        private DownloadProcess RegisterDownloadProcess(DownloadOptions options)
        {
            if (currentDownloads.Find((x) => x.DownloadOptions.FilePath == options.FilePath) != null)
            {
                Logger?.Log($"Trying download to file that already busy by another download process!", type: LogType.Error);
                return null;
            }

            DownloadProcess process = new DownloadProcess(options, client, Logger);
            currentDownloads.Add(process);
            process.DownloadEvent += HandleDownloadProcessEvent;
            return process;
        }

        private void HandleDownloadProcessEvent(DownloadEventArgs args)
        {
            if (args.State.Status == DownloadStatus.Started)
            {
                CreateBackupIfNeeded(args.Sender);
            }
            else if (args.State.IsDone)
            {
                currentDownloads.Remove(args.Sender);
                RemoveDownloadingFileIfNeededAsync(args.Sender);
            }
        }

        private void CreateBackupIfNeeded(DownloadProcess process)
        {
            var options = process.DownloadOptions;
            string path = options.FilePath;

            bool createBackupOption =
                options.FileCreationMode == FileCreationMode.CreateBackup ||
                options.FileCreationMode == FileCreationMode.CreateBackupAndAppend;

            if (!createBackupOption || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            Logger?.Log($"Creating backup for {path}");
            File.Copy(path, $"{path}.backup");
        }

        private async void RemoveDownloadingFileIfNeededAsync(DownloadProcess process)
        {
            bool canDelete = process.State.Status == DownloadStatus.Cancelled ||
                             process.State.Status == DownloadStatus.Failed;

            if (!process.DownloadOptions.DeleteOnCancelOrOnFail || !canDelete)
            {
                return;
            }

            string path = process.DownloadOptions.FilePath;

            while (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Logger?.Log($"File {path} removed");
                    break;
                }
                catch
                {
                    await UniTask.WaitForSeconds(0.1F);
                }
            }
        }

        public void Dispose()
        {
            foreach (DownloadProcess process in currentDownloads)
            {
                process.Dispose();
            }

            currentDownloads.Clear();
            client.Dispose();
        }
    }
}
