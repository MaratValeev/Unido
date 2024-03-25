using System;
using System.Collections.Generic;
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
