using System;
using System.Text;

namespace Unido
{
    public class DownloadEventArgs
    {
        public DownloadProcess Sender { get; set; }

        public DownloadStatus Status { get; set; }
        public Exception Exception { get; set; }
        public int StatusCode { get; set; }

        public Uri Url { get; set; }
        public string FilePath { get; set; }

        public float Progress { get; set; }
        public float DownloadSpeedAverage { get; set; }
        public float DownloadBytesPerSecond { get; set; }

        public long TotalBytesToDownload { get; set; }
        public long DownloadedBytesCount { get; set; }
        public long BytesToDownloadLeft => TotalBytesToDownload - DownloadedBytesCount;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            //TODO: nameof
            builder.AppendLine($"Url: {Url}");
            builder.AppendLine($"FilePath: {FilePath}");
            builder.AppendLine($"Status: {Status}");
            builder.AppendLine($"StatusCode: {StatusCode}");
            builder.AppendLine($"DownloadSpeedAverage: {DownloadSpeedAverage}");
            builder.AppendLine($"DownloadBytesPerSecond: {DownloadBytesPerSecond}");
            builder.AppendLine($"Progress: {Progress}");
            builder.AppendLine($"TotalBytesToDownload: {TotalBytesToDownload}");
            builder.AppendLine($"DownloadedBytesCount: {DownloadedBytesCount}");
            builder.AppendLine($"BytesToDownloadLeft: {BytesToDownloadLeft}");

            return builder.ToString();
        }
    }
}
