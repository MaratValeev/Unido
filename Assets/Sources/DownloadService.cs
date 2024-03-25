using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Unido
{
    public class DownloadServiceConfig
    {
        public float Timeout { get; set; } = 5;
        public ILogger Logger { get; set; } = new UnidoLogger();
    }

    public partial class DownloadService : IDisposable
    {
        private List<DownloadProcess> currentDownloads;
        private HttpClient client;

        public ILogger Logger { get; set; }
        public DownloadOptions DefaultDownloadOptions { get; private set; }
        public IReadOnlyCollection<DownloadProcess> CurrentDownloadProcesses => currentDownloads.AsReadOnly();

        public DownloadService() : this(new DownloadServiceConfig())
        {
        }

        public DownloadService(DownloadServiceConfig config)
        {
            if (config == null)
            {
                config = new DownloadServiceConfig();
            }

            Logger = config.Logger;
            client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(config.Timeout)
            };

            Logger.Log($"Initialize {nameof(DownloadService)}");
            currentDownloads = new List<DownloadProcess>();
            DefaultDownloadOptions = new DownloadOptions(Logger);
        }

        private DownloadProcess RegisterDownloadProcess(DownloadOptions options)
        {
            DownloadProcess process = new DownloadProcess(options, client);
            currentDownloads.Add(process);
            process.DownloadEvent += HandleDownloadProcessEvent;
            return process;
        }

        private void HandleDownloadProcessEvent(DownloadEventArgs args)
        {
            if (args.Status == DownloadStatus.Started)
            {
                CreateBackupIfNeeded(args.Sender);
            }
            else if (args.Status == DownloadStatus.Completed)
            {
                currentDownloads.Remove(args.Sender);
            }
        }

        private void CreateBackupIfNeeded(DownloadProcess process)
        {
            var options = process.DownloadOptions;
            string path = options.FilePath;
            if (!options.CreateBackup || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            Logger?.Log($"Creating backup for {path}");
            File.Copy(path, $"{path}.backup");
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
