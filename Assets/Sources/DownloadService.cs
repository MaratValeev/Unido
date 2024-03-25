using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Unido
{
    public class DownloadServiceConfig
    {
        public float Timeout { get; set; } = 5;
    }

    public partial class DownloadService : IDisposable
    {
        private List<DownloadProcess> currentDownloads;
        private HttpClient client;

        public ILogger Logger { get; set; }
        public DownloadOptions DefaultDownloadOptions { get; private set; }
        public event Action<DownloadEventArgs> DownloadEvent;

        public DownloadService() : this(new DownloadServiceConfig())
        {
        }

        public DownloadService(DownloadServiceConfig config)
        {
            Logger = new UnidoLogger();
            client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(config.Timeout)
            };

            Logger.Log($"Initialize {nameof(DownloadService)}");
            currentDownloads = new List<DownloadProcess>();
            DefaultDownloadOptions = new DownloadOptions();
        }

        private DownloadProcess RegisterDownloadProcess(DownloadOptions options)
        {
            DownloadProcess process = new DownloadProcess(options, client);
            currentDownloads.Add(process);
            return process;
        }

        public void Dispose()
        {
            foreach (DownloadProcess process in currentDownloads)
            {
                process.Dispose();
            }

            client.Dispose();
        }
    }
}
