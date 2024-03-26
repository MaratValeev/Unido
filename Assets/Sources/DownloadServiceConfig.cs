namespace Unido
{
    public class DownloadServiceConfig
    {
        public float Timeout { get; set; } = 5;
        public ILogger Logger { get; set; } = new UnidoLogger();
    }
}
