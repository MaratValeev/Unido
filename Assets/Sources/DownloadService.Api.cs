using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;

namespace Unido
{
    public partial class DownloadService
    {
        //With default options
        public DownloadProcess Download(string url)
        {
            var options = (DownloadOptions)DefaultDownloadOptions.Clone();
            options.Url = new Uri(url);

            return RegisterDownloadProcess(options);
        }

        public async Task DownloadAsync(string url)
        {
            var options = (DownloadOptions)DefaultDownloadOptions.Clone();
            options.Url = new Uri(url);

            var process = RegisterDownloadProcess(options);
            if (options.StartDownloadOnCreate)
            {
                await process.StartDownloadAsync().AsUniTask();
            }
        }

        public DownloadProcess DownloadAsFile(string url, string filePath)
        {
            var options = (DownloadOptions)DefaultDownloadOptions.Clone();
            options.Url = new Uri(url);
            options.FilePath = filePath;

            var process = RegisterDownloadProcess(options);

            if (options.StartDownloadOnCreate)
            {
                process.StartDownloadAsync().AsUniTask().Forget();
            }

            return process;
        }

        public async Task DownloadAsFileAsync(string url, string filePath)
        {
            var options = (DownloadOptions)DefaultDownloadOptions.Clone();
            options.Url = new Uri(url);
            options.FilePath = filePath;

            var process = RegisterDownloadProcess(options);
            await process.StartDownloadAsync().AsUniTask();
        }

        //With options
        public DownloadProcess Download(DownloadOptions options)
        {
            var process = RegisterDownloadProcess(options);
            if (options.StartDownloadOnCreate)
            {
                process.StartDownloadAsync().AsUniTask().Forget();
            }
            return process;
        }

        public async Task DownloadAsync(DownloadOptions options)
        {
            var process = RegisterDownloadProcess(options);
            await process.StartDownloadAsync();
        }

        public void GetDownloadProcess()
        {

        }
    }
}
