using System;

namespace Unido
{
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

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool Validate(out string message)
        {
            message = "Ok";
            return true;
        }
    }
}
