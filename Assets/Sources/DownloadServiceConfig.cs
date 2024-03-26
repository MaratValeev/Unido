using UnityEngine;

namespace Unido
{
    public class DownloadServiceConfig
    {
        public float Timeout { get; set; } = 5;
        public ILogger Logger { get; set; }

        public DownloadServiceConfig()
        {
            Logger = new UnidoLogger();
        }

        public DownloadServiceConfig(float timeout, ILogger logger)
        {
            Timeout = timeout;
            Logger = logger;
        }

        public DownloadServiceConfig(float timeout, GameObject logContext)
        {
            Timeout = timeout;
            Logger = new UnidoLogger(logContext);
        }
    }
}
