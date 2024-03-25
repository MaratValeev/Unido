using System;

namespace Unido
{
    public enum DownloadProgressChangeTrigger
    {
        ByDownloadedBytes,
        ByPrecentage,
        ByMilliseconds
    }

    public class DownloadOptions : ICloneable
    {
        //UNDONE
        public bool CreateBackup { get; set; } = true;
        public bool DeleteOnCancel { get; set; } = true;
        public Uri Url { get; set; }
        public string FilePath { get; set; }
        public ILogger Logger { get; set; }
        public bool StartDownloadOnCreate { get; set; } = true;
        //UNDONE
        public long? SpeedLimit { get; set; } = null;
        public int FileStreamBufferSize { get; set; } = 8192;

        //UNDONE
        public DownloadProgressChangeTrigger ProgressTriggerType { get; set; } = DownloadProgressChangeTrigger.ByMilliseconds;
        public int ProgressTriggerValue { get; set; }

        public DownloadOptions(ILogger logger = null)
        {
            Logger = logger;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool Validate(out string message)
        {
            //UNDONE
            message = "Ok";
            return true;
        }
    }
}
