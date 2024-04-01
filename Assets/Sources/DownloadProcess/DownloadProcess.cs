using Born2Code.Net;
using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unido
{
    /// <include file='Documentation.xml' path='docs/members[@name="DownloadProcess"]/*' />
    public class DownloadProcess : IDisposable
    {
        private HttpClient client;
        private ILogger logger;
        private DownloadProcessState state;

        private ThrottledStream downloadStream;
        private FileStream fileStream;

        private CancellationTokenSource cts;

        //TODO: to state
        private DateTime startDownloadDateTime;

        private DateTime lastDonwloadEventInvokeDateTime;
        private int streamReadDownloadEventCounter;
        private float progressDownloadEventCounter;

        public IDonwloadProcessState State => state;
        public DownloadOptions DownloadOptions { get; private set; }
        public event Action<DownloadEventArgs> DownloadEvent;

        public DownloadProcess(DownloadOptions downloadOptions, HttpClient client, ILogger logger)
        {
            this.logger = logger;
            this.client = client;
            DownloadOptions = downloadOptions;

            state = new DownloadProcessState();

            if (!downloadOptions.CheckValidity(out string validateResultMessage))
            {
                string exceptionMessage = $"Invalid download options: {validateResultMessage}";
                Exception argException = new ArgumentException(exceptionMessage);
                logger?.Log(argException);
                state.IsValid = false;
                return;
            }

            logger?.Log($"Validate options successful done.");
            state.IsValid = true;
            state.Status = DownloadStatus.NotStarted;
        }

        public async Task StartDownloadAsync()
        {
            if (!state.IsValid)
            {
                return;
            }

            if (state.Status != DownloadStatus.NotStarted)
            {
                logger?.Log($"Tried start download that already started before!", type: LogType.Warning);
                return;
            }

            startDownloadDateTime = DateTime.Now;

            //Invoke event of start download
            InvokeDownloadEvent();

            cts = new CancellationTokenSource();

            //Invoke event of start head request
            InvokeDownloadEvent();

            using var headResponse = await TrySendHeadRequestAsync();
            if (headResponse == null)
            {
                return;
            }

            HandleHeadResponse(headResponse, out bool downloadDontNeeded);
            if (downloadDontNeeded)
            {
                return;
            }

            SetupContinueModeIfNeeded(headResponse, out downloadDontNeeded);
            if (downloadDontNeeded)
            {
                return;
            }

            using var getResponse = await TrySendGetRequestAsync();
            if (getResponse == null)
            {
                return;
            }

            logger?.Log($"Successful start download {DownloadOptions.Url}. Status code: {getResponse.StatusCode}");
            await DownloadContentFromHttpResponseMessage(getResponse);
        }

        private void SetupContinueModeIfNeeded(HttpResponseMessage headResponse, out bool downloadDontNeeded)
        {
            downloadDontNeeded = false;

            if (DownloadOptions.FileCreationMode != FileCreationMode.TryContinue)
            {
                return;
            }

            FileInfo info = new FileInfo(DownloadOptions.FilePath);
            state.DownloadedBytesCount = info.Length;

            if (IsSameSize())
            {
                logger?.Log($"Continue download {DownloadOptions.Url} does not needed because " +
                            $"target file size and local file size is identical");

                downloadDontNeeded = true;
                InvokeDownloadEvent();
            }
        }

        private async Task<HttpResponseMessage> TrySendHeadRequestAsync()
        {
            using HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Head,
                RequestUri = DownloadOptions.Url
            };

            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };

            if (!string.IsNullOrEmpty(DownloadOptions.ETag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(DownloadOptions.ETag));
            }

            return await ExecuteRequest(request);
        }

        private async Task<HttpResponseMessage> TrySendGetRequestAsync()
        {
            using HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = DownloadOptions.Url
            };

            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            if (DownloadOptions.FileCreationMode == FileCreationMode.TryContinue)
            {
                FileInfo info = new FileInfo(DownloadOptions.FilePath);
                request.Headers.Range = new RangeHeaderValue(info.Length, null);
            }

            return await ExecuteRequest(request);
        }

        private async Task<HttpResponseMessage> ExecuteRequest(HttpRequestMessage request)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                state.ResponseStatusCode = (int)response.StatusCode;
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception ex)
            {
                InvokeDownloadEvent(ex);
                return null;
            }
        }

        private void HandleHeadResponse(HttpResponseMessage response, out bool downloadDontNeeded)
        {
            downloadDontNeeded = false;

            state.TotalFileSize = response.Content.Headers.ContentLength;

            if (!response.Headers.AcceptRanges.Contains("bytes"))
            {
                logger?.Log("Server doesn't support download range content by bytes, " +
                           $"download will continue in '{FileCreationMode.Replace}' mode.", type: LogType.Warning);
                DownloadOptions.FileCreationMode = FileCreationMode.Replace;
            }

            state.ETag = response.Headers.ETag.Tag;

            if (!string.IsNullOrEmpty(DownloadOptions.ETag) && response.StatusCode == HttpStatusCode.NotModified)
            {
                logger?.Log($"Download {DownloadOptions.Url} does not needed because " +
                            $"remote file is not modified");

                downloadDontNeeded = true;
                InvokeDownloadEvent();
            }
        }

        private bool IsSameSize()
        {
            return state.TotalFileSize == state.DownloadedBytesCount;
        }

        private async Task DownloadContentFromHttpResponseMessage(HttpResponseMessage response)
        {
            state.TotalFileSize = response.Content.Headers.ContentLength;
            logger?.Log($"Downloading bytes count: {state.TotalFileSize}");

            Stream responseStream;
            try
            {
                responseStream = await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                InvokeDownloadEvent(ex);
                return;
            }

            downloadStream = new ThrottledStream(responseStream);
            downloadStream.MaximumBytesPerSecond = DownloadOptions.SpeedLimit;

            await ReadDownloadStream();
        }

        private async Task ReadDownloadStream()
        {
            int bufferSize = DownloadOptions.BufferSize;
            var buffer = new byte[bufferSize];
            string filePath = DownloadOptions.FilePath;
            DateTime lastSpeedUpdate = DateTime.Now;
            lastDonwloadEventInvokeDateTime = DateTime.Now;
            long lastSpeedUpdateDownloadedBytes = 0;

            try
            {
                var mode = SelectFileMode(DownloadOptions.FileCreationMode);
                fileStream = new FileStream(filePath, mode, FileAccess.Write, FileShare.None, bufferSize, true);
            }
            catch (Exception ex)
            {
                InvokeDownloadEvent(ex);
                return;
            }

            state.Status = DownloadStatus.DownloadingContent;

            do
            {
                if (state.Paused)
                {
                    await UniTask.Yield();
                    continue;
                }

                int bytesToRead;
                try
                {
                    bytesToRead = await downloadStream.ReadAsync(buffer, 0, bufferSize, cts.Token);
                    if (bytesToRead == 0)
                    {
                        continue;
                    }

                    state.DownloadedBytesCount += bytesToRead;
                    streamReadDownloadEventCounter++;
                }
                catch (Exception ex)
                {
                    InvokeDownloadEvent(ex);
                    return;
                }

                var now = DateTime.Now;

                if ((now - lastSpeedUpdate).TotalSeconds >= 1.0F)
                {
                    state.DownloadedBytesForLastSecond = state.DownloadedBytesCount - lastSpeedUpdateDownloadedBytes;
                    lastSpeedUpdate = now;
                    lastSpeedUpdateDownloadedBytes = state.DownloadedBytesCount;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    try
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesToRead, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        InvokeDownloadEvent(ex);
                        return;
                    }
                }

                CalculateAndSetDownloadProgress();
                if (IsNeedInvokeProgressChangeEvent())
                {
                    InvokeDownloadEvent();
                }
            }
            while (state.Status == DownloadStatus.DownloadingContent);
        }

        private FileMode SelectFileMode(FileCreationMode mode)
        {
            switch (mode)
            {
                case FileCreationMode.Replace:
                    return FileMode.Create;

                default:
                case FileCreationMode.CreateBackupAndTryContinue:
                case FileCreationMode.TryContinue:
                    return FileMode.Append;
            }
        }

        private bool IsNeedInvokeProgressChangeEvent()
        {
            float triggerValue = DownloadOptions.ProgressTriggerValue;

            switch (DownloadOptions.ProgressTriggerType)
            {
                case DownloadProgressChangeTrigger.ByStreamReadCounts:
                    if (streamReadDownloadEventCounter % triggerValue == 0)
                    {
                        return true;
                    }
                    break;

                case DownloadProgressChangeTrigger.ByMilliseconds:
                    DateTime now = DateTime.Now;
                    if ((now - lastDonwloadEventInvokeDateTime).TotalMilliseconds >= triggerValue)
                    {
                        lastDonwloadEventInvokeDateTime = now;
                        return true;
                    }
                    break;

                case DownloadProgressChangeTrigger.ByPrecentage:
                    float precentage = state.Progress * 100;
                    if (precentage - progressDownloadEventCounter >= triggerValue)
                    {
                        progressDownloadEventCounter = precentage;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private void InvokeDownloadEvent(Exception exception = null)
        {
            DefineAndSetCurrentStatus(exception);
            CalculateAndSetDownloadSpeedAverage();

            DownloadEventArgs eventArgs = new DownloadEventArgs()
            {
                Sender = this,
                Exception = exception,
                State = State,
                Options = DownloadOptions
            };

            DownloadEvent?.Invoke(eventArgs);
        }

        private void DefineAndSetCurrentStatus(Exception exception)
        {
            DownloadStatus status = DownloadStatus.Undefined;

            if (exception != null)
            {
                status = HandleException(exception);
            }
            else if (State.Status == DownloadStatus.NotStarted)
            {
                status = DownloadStatus.Started;
            }
            else if (State.Status == DownloadStatus.Started)
            {
                status = DownloadStatus.SendingHeadRequest;
            }
            else if (State.Status == DownloadStatus.SendingHeadRequest)
            {
                status = DownloadStatus.SendingGetRequest;
            }
            else
            {
                if (state.DownloadedBytesCount < state.TotalFileSize.Value)
                {
                    if (state.DownloadedBytesCount > 0)
                    {
                        status = DownloadStatus.DownloadingContent;
                    }
                }
                else if (IsSameSize())
                {
                    logger?.Log($"File download process completed.\n" +
                        $"Url: {DownloadOptions.Url}");

                    DisposeStreams();
                    cts.Dispose();

                    status = DownloadStatus.Completed;
                }
                else
                {
                    throw new InvalidOperationException("Downloaded bytes is more than total!");
                }
            }

            state.Status = status;
        }

        private DownloadStatus HandleException(Exception exception)
        {
            bool canceled = exception is OperationCanceledException;

            WebException webException = exception is WebException ? exception as WebException : null;
            if (webException != null && webException.Status == WebExceptionStatus.RequestCanceled)
            {
                canceled = true;
            }

            DisposeStreams();
            cts.Dispose();

            if (canceled)
            {
                logger?.Log($"File download process canceled.\n" +
                    $"Url: {DownloadOptions.Url}");

                return DownloadStatus.Cancelled;
            }
            else
            {
                logger?.Log($"File download process failed.\n" +
                    $"Url:{DownloadOptions.Url}\n" +
                    $"Exception: {exception}",
                    type: LogType.Exception);

                return DownloadStatus.Failed;
            }
        }

        private void CalculateAndSetDownloadSpeedAverage()
        {
            var elapsedMilliseconds = (DateTime.Now - startDownloadDateTime).TotalSeconds;
            if (elapsedMilliseconds == 0)
            {
                state.DownloadSpeedAverage = 0;
            }
            state.DownloadSpeedAverage = state.DownloadedBytesCount / (float)elapsedMilliseconds;
        }

        private void CalculateAndSetDownloadProgress()
        {
            if (state.TotalFileSize.HasValue)
            {
                state.Progress = (float)state.DownloadedBytesCount / state.TotalFileSize.Value;
            }
            else
            {
                state.Progress = 0;
            }
        }

        public async void CancelDownload()
        {
            if (state.Status != DownloadStatus.DownloadingContent || state.Status != DownloadStatus.Started)
            {
                return;
            }

            cts.Cancel();
            await UniTask.WaitUntil(() => state.Status == DownloadStatus.Cancelled);
            DisposeStreams();
            cts.Dispose();
        }

        public void Dispose()
        {
            CancelDownload();
            client = null;
            state.IsValid = false;
        }

        private void DisposeStreams()
        {
            if (downloadStream != null)
            {
                downloadStream.Dispose();
                downloadStream = null;
            }

            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }
        }
    }
}
