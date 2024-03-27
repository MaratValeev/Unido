using System.Text;

namespace Unido
{
    internal class DownloadProcessState : IDonwloadProcessState
    {
        public DownloadStatus Status { get; set; }
        public bool IsValid { get; set; }
        public long? TotalFileSize { get; set; }
        public long DownloadedBytesCount { get; set; }
        public float Progress { get; set; }
        public float DownloadSpeedAverage { get; set; }
        public float DownloadedBytesForLastSecond { get; set; }
        public bool IsDone
        {
            get
            {
                return Status == DownloadStatus.Completed ||
                    Status == DownloadStatus.Cancelled ||
                    Status == DownloadStatus.Failed;
            }
        }
        public int StatusCode { get; set; }
        public bool Paused { get; set; }
        public long BytesToDownloadLeft
        {
            get
            {
                if (TotalFileSize.HasValue)
                {
                    return TotalFileSize.Value - DownloadedBytesCount;
                }
                return 0;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"{nameof(Status)}: {Status}");
            builder.AppendLine($"{nameof(IsValid)}: {IsValid}");
            builder.AppendLine($"{nameof(TotalFileSize)}: {TotalFileSize}");
            builder.AppendLine($"{nameof(DownloadedBytesCount)}: {DownloadedBytesCount}");
            builder.AppendLine($"{nameof(Progress)}: {Progress}");
            builder.AppendLine($"{nameof(DownloadSpeedAverage)}: {DownloadSpeedAverage}");
            builder.AppendLine($"{nameof(DownloadedBytesForLastSecond)}: {DownloadedBytesForLastSecond}");
            builder.AppendLine($"{nameof(StatusCode)}: {StatusCode}");
            builder.AppendLine($"{nameof(BytesToDownloadLeft)}: {BytesToDownloadLeft}");

            return builder.ToString();
        }
    }

    public interface IDonwloadProcessState
    {
        public DownloadStatus Status { get; }
        public bool IsValid { get; }
        public long? TotalFileSize { get; }
        public long DownloadedBytesCount { get; }
        public float Progress { get; }
        public float DownloadSpeedAverage { get; }
        public float DownloadedBytesForLastSecond { get; }
        public bool IsDone { get; }
        public int StatusCode { get; }
        public long BytesToDownloadLeft { get; }
        public bool Paused { get; set; }
    }
}
