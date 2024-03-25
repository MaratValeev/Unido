using System;

namespace Unido
{
    public class DownloadEventArgs
    {
        public DownloadProcess Sender { get; set; }

        public DownloadStatus Status { get; set; }
        public Exception Exception { get; set; }
        public int StatusCode { get; set; }

        public string Url { get; set; }
        public string FilePath { get; set; }

        public float Progress { get; set; }
        public float DownloadSpeed { get; set; }
        public long BytesToDownload { get; set; }
        public long TotalBytesToDownload { get; set; }
        public byte[] DownloadedBytes { get; set; }

        public long DownloadedBytesCount => DownloadedBytes.LongLength;
    }
}
