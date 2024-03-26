using System;
using System.Text;
using UnityEngine;

namespace Unido
{
    public enum FileCreationMode
    {
        CreateBackup,
        Replace,
        TryContinue,
        CreateBackupAndAppend
    }

    /// <include file='Documentation.xml' path='docs/members[@name="DownloadOptions"]/*' />
    public class DownloadOptions : ICloneable
    {
        private long speedLimit = 0;
        private int bufferSize = 4096;
        private float progressTriggerValue = 250;

        public FileCreationMode FileCreationMode { get; set; } = FileCreationMode.TryContinue;
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
        public GameObject Context { get; set; }
        public DownloadProgressChangeTrigger ProgressTriggerType { get; set; } = DownloadProgressChangeTrigger.ByMilliseconds;
        public float ProgressTriggerValue
        {
            get { return progressTriggerValue; }
            set
            {
                progressTriggerValue = Mathf.Clamp(value, 0, float.MaxValue);
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool CheckValidity(out string message)
        {
            if (string.IsNullOrEmpty(Url.AbsoluteUri))
            {
                message = "URL is null or empty!";
                return false;
            }

            if (!(Url.Scheme == Uri.UriSchemeHttp || Url.Scheme == Uri.UriSchemeHttps))
            {
                message = "Invalid URL scheme!";
                return false;
            }

            if (!Uri.IsWellFormedUriString(Url.AbsoluteUri, UriKind.Absolute))
            {
                message = "Invalid URL path!";
                return false;
            }

            if (FileCreationMode == FileCreationMode.TryContinue && string.IsNullOrEmpty(FilePath))
            {
                message = "Need set file path for try continue download!";
                return false;
            }

            message = "Ok";
            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"{nameof(FileCreationMode)}: {FileCreationMode}");
            builder.AppendLine($"{nameof(DeleteOnCancelOrOnFail)}: {DeleteOnCancelOrOnFail}");
            builder.AppendLine($"{nameof(Url)}: {Url}");
            builder.AppendLine($"{nameof(FilePath)}: {FilePath}");
            builder.AppendLine($"{nameof(StartDownloadOnCreate)}: {StartDownloadOnCreate}");
            builder.AppendLine($"{nameof(SpeedLimit)}: {SpeedLimit}");
            builder.AppendLine($"{nameof(BufferSize)}: {BufferSize}");
            builder.AppendLine($"{nameof(Context)}: {Context}");

            return builder.ToString();
        }
    }
}
