using System;
using System.IO;

namespace Unido
{
    /// <include file='Documentation.xml' path='docs/members[@name="DownloadOptions"]/*' />
    public class DownloadOptions : ICloneable
    {
        private long speedLimit = 0;
        private int bufferSize = 4096;

        public bool CreateBackup { get; set; } = false;
        public bool DeleteOnCancelOrOnFail { get; set; } = true;
        public Uri Url { get; set; }
        public string FilePath { get; set; }
        public bool StartDownloadOnCreate { get; set; } = true;
        public long SpeedLimit
        {
            get { return speedLimit; }
            set
            {
                if (value < 0)
                {
                    value = 16;
                }

                speedLimit = value;
            }
        }
        public int BufferSize
        {
            get { return bufferSize; }
            set
            {
                if (value < 0)
                {
                    value = 16;
                }

                bufferSize = value;
            }
        }

        public FileMode FileMode { get; set; } = FileMode.Append;

        //UNDONE
        public DownloadProgressChangeTrigger ProgressTriggerType { get; set; } = DownloadProgressChangeTrigger.ByMilliseconds;
        //UNDONE
        public int ProgressTriggerValue { get; set; } = 250;

        public object Clone()
        {
            return MemberwiseClone();
        }

        //TODO: add recomendations
        public bool Validate(out string message)
        {
            if (string.IsNullOrEmpty(Url.AbsoluteUri))
            {
                message = "URL is null or empty";
                return false;
            }

            if (!(Url.Scheme == Uri.UriSchemeHttp || Url.Scheme == Uri.UriSchemeHttps))
            {
                message = "Invalid URL scheme";
                return false;
            }

            if (!Uri.IsWellFormedUriString(Url.AbsoluteUri, UriKind.Absolute))
            {
                message = "Invalid URL path";
                return false;
            }

            message = "Ok";
            return true;
        }
    }
}
