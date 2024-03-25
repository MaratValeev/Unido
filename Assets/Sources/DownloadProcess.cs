using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Unido
{
    public class DownloadProcess : IDisposable
    {
        public DownloadOptions DownloadOptions { get; private set; }
        public DownloadStatus Status { get; private set; }
        public bool IsValid { get; private set; }
        public Stream DownloadStream { get; private set; }

        public event Action<DownloadEventArgs> DownloadEvent;

        private DownloadEventArgs cachedDownloadEventArgs;
        private Task<HttpResponseMessage> downloadTask;
        private HttpClient client;
        private ILogger logger => DownloadOptions.Logger;

        public DownloadProcess(DownloadOptions options, HttpClient client)
        {
            if (!options.Validate(out string validateResultMessage))
            {
                options.Logger?.Log($"Options validate failed: {validateResultMessage}");
                IsValid = false;
                return;
            }

            options.Logger?.Log($"Options validate successful done");
            DownloadOptions = options;
            this.client = client;
            IsValid = true;
        }

        public async Task DownloadAsync()
        {
            if (!IsValid) return;

            if (Status == DownloadStatus.InProgress)
            {
                return;
            }

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = DownloadOptions.Url
            };

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            DownloadStream = await response.Content.ReadAsStreamAsync();

            await ProcessContentStream(totalBytes);
        }

        private async Task ProcessContentStream(long? totalDownloadSize)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            string filePath = DownloadOptions.FilePath;
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true);

            do
            {
                var bytesRead = await DownloadStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                    continue;
                }

                await fileStream.WriteAsync(buffer, 0, bytesRead);

                totalBytesRead += bytesRead;
                readCount += 1;

                if (readCount % 100 == 0)
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
            }
            while (isMoreToRead);
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {

        }

        public void CancelDownload()
        {
            if (!IsValid) return;

            if (Status == DownloadStatus.Cancelled)
            {
                return;
            }

            bool isFileExist = File.Exists(DownloadOptions.FilePath);
            if (DownloadOptions.DeleteOnCancel && isFileExist)
            {
                File.Delete(DownloadOptions.FilePath);
            }
        }

        public void ResumeDownload()
        {
            if (!IsValid) return;

        }

        public void PauseDownload()
        {
            if (!IsValid) return;

        }

        public void Dispose()
        {
            CancelDownload();
        }
    }
}
