using System;
using System.Text;

namespace Unido
{
    public class DownloadEventArgs
    {
        public DownloadProcess Sender { get; set; }
        public IDonwloadProcessState State { get; set; }
        public DownloadOptions Options { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"Download process state:");
            builder.AppendLine(State.ToString());
            builder.AppendLine($"Download options:");
            builder.AppendLine(Options.ToString());

            return builder.ToString();
        }
    }
}
