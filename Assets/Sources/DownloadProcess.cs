using Born2Code.Net;
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
        public float DownloadSpeedAverage { get; private set; }
        public float DownloadSpeedBytesPerSecond { get; private set; }

        public event Action<DownloadEventArgs> DownloadEvent;

        private ILogger logger;
        private int statusCode;
        private ThrottledStream downloadStream;
        private FileStream fileStream;
        private HttpClient client;
        private CancellationTokenSource cts;
        private DateTime startDownloadDateTime;

        public DownloadProcess(DownloadOptions options, HttpClient client, ILogger logger)
        {
            this.logger = logger;

            if (!options.Validate(out string validateResultMessage))
            {
                logger?.Log($"Validate options failed: {validateResultMessage}");
                IsValid = false;
                return;
            }

            logger?.Log($"Validate options successful done");
            IsValid = true;

            this.client = client;

            Status = DownloadStatus.NotStarted;
            Progress = -1;
            DownloadOptions = options;
        }

        public async Task StartDownloadAsync()
        {
            if (!IsValid) return;

            if (Status != DownloadStatus.NotStarted)
            {
                logger?.Log($"Tried start download that already started before!");
                return;
            }

            InvokeDownloadEvent();

            startDownloadDateTime = DateTime.Now;
            cts = new CancellationTokenSource();

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

            var stream = await response.Content.ReadAsStreamAsync();

            downloadStream = new ThrottledStream(stream);
            downloadStream.MaximumBytesPerSecond = DownloadOptions.SpeedLimit;

            await ReadDownloadStream();
        }

        private async Task ReadDownloadStream()
        {
            int bufferSize = DownloadOptions.BufferSize;
            var buffer = new byte[bufferSize];
            int readCounts = 0;
            string filePath = DownloadOptions.FilePath;

            DateTime lastSpeedUpdate = DateTime.Now;
            long lastSpeedUpdateDownloadedBytes = 0;

            fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, true);

            Status = DownloadStatus.InProgress;

            do
            {
                if (Status != DownloadStatus.InProgress)
                {
                    break;
                }

                var bytesToRead = await downloadStream.ReadAsync(buffer, 0, bufferSize, cts.Token);
                DownloadedBytesCount += bytesToRead;

                if ((DateTime.Now - lastSpeedUpdate).TotalSeconds >= 1)
                {
                    DownloadSpeedBytesPerSecond = DownloadedBytesCount - lastSpeedUpdateDownloadedBytes;
                    lastSpeedUpdate = DateTime.Now;
                    lastSpeedUpdateDownloadedBytes = DownloadedBytesCount;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    await fileStream.WriteAsync(buffer, 0, bytesToRead, cts.Token);
                }

                if (IsNeedInvokeProgressChangeEvent(readCounts))
                {
                    InvokeDownloadEvent();
                }
            }
            while (true);
        }

        private bool IsNeedInvokeProgressChangeEvent(int readCounts)
        {
            int triggerValue = DownloadOptions.ProgressTriggerValue;

            switch (DownloadOptions.ProgressTriggerType)
            {
                case DownloadProgressChangeTrigger.ByBufferCounts:
                    if (readCounts % triggerValue == 0)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private void InvokeDownloadEvent(Exception exception = null)
        {
            DownloadStatus status;
            if (exception != null)
            {
                status = HandleException(exception);
            }
            else
            {
                status = GetStatus();
            }

            Status = status;

            CalculateAndSetDownloadProgress();
            CalculateAndSetDownloadSpeedAverage();

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
                DownloadSpeedAverage = DownloadSpeedAverage,
                DownloadBytesPerSecond = DownloadSpeedBytesPerSecond
            };

            DownloadEvent?.Invoke(eventArgs);
        }

        private DownloadStatus GetStatus()
        {
            DownloadStatus status = DownloadStatus.Undefined;

            if (TotalFileSize.HasValue)
            {
                if (DownloadedBytesCount < TotalFileSize.Value)
                {
                    if (DownloadedBytesCount > 0)
                    {
                        status = DownloadStatus.InProgress;
                    }
                }
                else if (DownloadedBytesCount == TotalFileSize.Value)
                {
                    logger?.Log($"Downloading file completed\n" +
                        $"Url: {DownloadOptions.Url}");

                    status = DownloadStatus.Completed;
                    CloseStreams();
                }
                else
                {
                    throw new InvalidOperationException("Downloaded bytes is more than total!");
                }
            }
            else
            {
                status = DownloadStatus.Started;
            }

            return status;
        }

        private DownloadStatus HandleException(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                logger?.Log($"Downloading file canceled.\n" +
                    $"Url: {DownloadOptions.Url}");

                return DownloadStatus.Cancelled;
            }
            else
            {
                logger?.Log($"Downloading file failed.\n" +
                    $"Url:{DownloadOptions.Url}\n" +
                    $"Exception: {exception}");

                return DownloadStatus.Failed;
            }
        }

        private void CalculateAndSetDownloadSpeedAverage()
        {
            var elapsedMs = (DateTime.Now - startDownloadDateTime).TotalSeconds;
            DownloadSpeedAverage = DownloadedBytesCount / (float)elapsedMs;
        }

        private void CalculateAndSetDownloadSpeed()
        {
            var elapsedMs = (DateTime.Now - startDownloadDateTime).TotalSeconds;
            DownloadSpeedAverage = DownloadedBytesCount / (float)elapsedMs;
        }

        private void CalculateAndSetDownloadProgress()
        {
            if (TotalFileSize.HasValue)
            {
                Progress = (float)DownloadedBytesCount / TotalFileSize.Value;
            }
            else
            {
                Progress = 0;
            }
        }

        //UNDONE
        public void ResumeDownload()
        {
            if (!IsValid) return;
        }

        //UNDONE
        public void PauseDownload()
        {
            if (!IsValid) return;
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

        public void Dispose()
        {
            CancelAndClear();
            client = null;
        }

        private void CancelAndClear()
        {
            cts?.Cancel();
            CloseStreams();
            cts?.Dispose();
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
            if (!IsValid) return;

            string path = DownloadOptions.FilePath;
            bool isFileExist = File.Exists(path);

            bool needDelete = Status == DownloadStatus.Cancelled || Status == DownloadStatus.Failed;
            if (needDelete && DownloadOptions.DeleteOnCancelOrOnFail && isFileExist)
            {
                File.Delete(path);
                logger?.Log($"File {path} removed");
            }
        }
    }
}
