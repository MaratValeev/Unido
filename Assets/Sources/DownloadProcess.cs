using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Unido
{
    public class DownloadProcess : IDisposable
    {
        public DownloadOptions DownloadOptions { get; private set; }
        public DownloadStatus Status { get; private set; }
        public bool IsValid { get; private set; }
        public long? TotalFileSize { get; private set; }
        public long DownloadedBytesCount { get; private set; }
        public float Progress { get; private set; }

        public event Action<DownloadEventArgs> DownloadEvent;

        private int statusCode;
        private Stream downloadStream;
        private FileStream fileStream;
        private HttpClient client;
        private CancellationTokenSource cts;

        private ILogger logger => DownloadOptions.Logger;

        public DownloadProcess(DownloadOptions options, HttpClient client)
        {
            if (!options.Validate(out string validateResultMessage))
            {
                options.Logger?.Log($"Validate options failed: {validateResultMessage}");
                IsValid = false;
                return;
            }

            Status = DownloadStatus.NotStarted;

            DownloadOptions = options;
            logger?.Log($"Validate options successful done");
            this.client = client;
            IsValid = true;
        }

        public async Task StartDownloadAsync()
        {
            if (!IsValid) return;

            if (Status != DownloadStatus.NotStarted)
            {
                logger?.Log($"Tried start download that already started!");
                return;
            }

            cts = new CancellationTokenSource();
            Status = DownloadStatus.InProgress;

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = DownloadOptions.Url
            };

            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                statusCode = (int)response.StatusCode;
                response.EnsureSuccessStatusCode();
                logger?.Log($"Successful start download {DownloadOptions.Url}. Status code: {response.StatusCode}");
                await DownloadFileFromHttpResponseMessage(response);
            }
            catch (Exception ex)
            {
                InvokeDownloadEvent(ex);
            }
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            TotalFileSize = response.Content.Headers.ContentLength;
            logger?.Log($"Downloading file size: {TotalFileSize}");

            downloadStream = await response.Content.ReadAsStreamAsync();

            await ReadDownloadStream();
        }

        private async Task ReadDownloadStream()
        {
            long readCount = 0;
            int bufferSize = DownloadOptions.FileStreamBufferSize;
            bool isMoreToRead = true;
            var buffer = new byte[bufferSize];

            string filePath = DownloadOptions.FilePath;

            fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

            do
            {
                var bytesToRead = await downloadStream.ReadAsync(buffer, 0, bufferSize, cts.Token);

                if (bytesToRead == 0)
                {
                    InvokeDownloadEvent();
                    break;
                }

                if (!string.IsNullOrEmpty(DownloadOptions.FilePath))
                {
                    await fileStream.WriteAsync(buffer, 0, bytesToRead, cts.Token);
                }

                DownloadedBytesCount += bytesToRead;
                readCount += 1;

                if (readCount % 100 == 0)
                {
                    InvokeDownloadEvent();
                }
            }
            while (isMoreToRead);
        }

        private void InvokeDownloadEvent(Exception exception = null)
        {
            DownloadStatus status = DownloadStatus.NotStarted;

            if (exception != null)
            {
                if (exception is OperationCanceledException)
                {
                    logger?.Log($"Downloading file canceled.\n" +
                        $"Url: {DownloadOptions.Url}");
                    status = DownloadStatus.Cancelled;
                }
                else
                {
                    logger?.Log($"Downloading file failed.\n" +
                        $"Url:{DownloadOptions.Url}\n" +
                        $"Exception: {exception}");


                    status = DownloadStatus.Failed;
                }
            }
            else if (DownloadedBytesCount < TotalFileSize.Value)
            {
                status = DownloadStatus.InProgress;
            }
            else if (DownloadedBytesCount == TotalFileSize.Value)
            {
                logger?.Log($"Downloading file completed.\n" +
                    $"Url: {DownloadOptions.Url}");
                status = DownloadStatus.Completed;
                CloseStreams();
            }

            Status = status;

            if (TotalFileSize.HasValue)
            {
                Progress = (float)DownloadedBytesCount / TotalFileSize.Value;
            }
            else
            {
                Progress = 0;
            }

            DownloadEventArgs eventArgs = new DownloadEventArgs()
            {
                Sender = this,
                FilePath = DownloadOptions.FilePath,
                Url = DownloadOptions.Url,
                Exception = exception,
                DownloadedBytesCount = DownloadedBytesCount,
                Progress = Progress,
                StatusCode = statusCode,
                Status = Status,
                TotalBytesToDownload = TotalFileSize.HasValue ? TotalFileSize.Value : 0,
                DownloadSpeed = CalculateDownloadSpeed()
            };

            DownloadEvent?.Invoke(eventArgs);
        }

        private long CalculateDownloadSpeed()
        {
            return 0;
        }

        public void CancelDownload()
        {
            if (!IsValid) return;

            if (Status != DownloadStatus.InProgress && Status != DownloadStatus.Paused)
            {
                logger?.Log($"Canceling download that not paused or in progress! Url: {DownloadOptions.Url}");
                return;
            }

            logger?.Log($"Cancel download {DownloadOptions.Url}");

            CancelAndClear();
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
            CancelAndClear();
        }

        private void CancelAndClear()
        {
            cts.Cancel();
            CloseStreams();
            cts.Dispose();
            cts = null;
            RemoveDownloadingFileIfNeeded();
        }

        private void CloseStreams()
        {
            if (downloadStream != null)
            {
                downloadStream.Close();
                downloadStream = null;
            }

            if (fileStream != null)
            {
                fileStream.Close();
                fileStream = null;
            }
        }

        private void RemoveDownloadingFileIfNeeded()
        {
            string path = DownloadOptions.FilePath;
            bool isFileExist = File.Exists(path);

            bool needDelete = Status == DownloadStatus.Cancelled || Status == DownloadStatus.Failed;
            if (needDelete && DownloadOptions.DeleteOnCancel && isFileExist)
            {
                File.Delete(path);
                logger?.Log($"File {path} removed");
            }
        }
    }
}
