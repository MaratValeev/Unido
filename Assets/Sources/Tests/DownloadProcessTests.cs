using System;
using System.Collections;
using System.Net.Http;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unido.Tests
{
    public class DownloadProcessTests
    {
        private string sampleUrl = "https://sample-videos.com/img/Sample-jpg-image-20mb.jpg​";

        [Test]
        public void InitialDownloadProcessStateTest()
        {
            using HttpClient client = new HttpClient();
            DownloadOptions options = new DownloadOptions()
            {
                Url = new Uri(sampleUrl),
            };

            using DownloadProcess process = new DownloadProcess(options, client, null);
            Assert.IsTrue(process.State.Status == DownloadStatus.NotStarted);
        }

        [Test]
        public void StartDownloadProcessStateTest()
        {
            using HttpClient client = new HttpClient();
            DownloadOptions options = new DownloadOptions()
            {
                Url = new Uri(sampleUrl),
            };

            using DownloadProcess process = new DownloadProcess(options, client, null);
            Assert.IsTrue(process.State.Status == DownloadStatus.NotStarted);
            _ = process.StartDownloadAsync();
            Assert.IsTrue(process.State.Status == DownloadStatus.SendingHeadRequest);
            process.CancelDownload();
        }

        public void StartDownloadProcessStateChangeEventCountsTest()
        {
            using HttpClient client = new HttpClient();
            DownloadOptions options = new DownloadOptions()
            {
                Url = new Uri(sampleUrl),
            };

            using DownloadProcess process = new DownloadProcess(options, client, null);

            int startedStatusInvokeEventCounts = 0;
            int sendingHeadRequestStatusInvokeEventCounts = 0;
            int SendingGetRequestStatusInvokeEventCounts = 0;

            process.DownloadEvent += (a) =>
            {
                Assert.IsFalse(a.State.Status == DownloadStatus.Undefined);

                switch (a.State.Status)
                {
                    case DownloadStatus.Started:
                        Assert.AreEqual(startedStatusInvokeEventCounts, 0);
                        Assert.AreEqual(sendingHeadRequestStatusInvokeEventCounts, 0);
                        Assert.AreEqual(SendingGetRequestStatusInvokeEventCounts, 0);
                        startedStatusInvokeEventCounts++;
                        break;

                    case DownloadStatus.SendingHeadRequest:
                        Assert.AreEqual(startedStatusInvokeEventCounts, 1);
                        Assert.AreEqual(sendingHeadRequestStatusInvokeEventCounts, 0);
                        Assert.AreEqual(SendingGetRequestStatusInvokeEventCounts, 0);
                        sendingHeadRequestStatusInvokeEventCounts++;
                        break;

                    case DownloadStatus.SendingGetRequest:
                        Assert.AreEqual(startedStatusInvokeEventCounts, 1);
                        Assert.AreEqual(sendingHeadRequestStatusInvokeEventCounts, 1);
                        Assert.AreEqual(SendingGetRequestStatusInvokeEventCounts, 0);
                        SendingGetRequestStatusInvokeEventCounts++;
                        break;

                    case DownloadStatus.DownloadingContent:
                        process.CancelDownload();
                        process.Dispose();
                        break;
                }
            };

            Assert.AreEqual(SendingGetRequestStatusInvokeEventCounts, 1);

            _ = process.StartDownloadAsync();
        }

        [UnityTest]
        public IEnumerator DownloadTestsWithEnumeratorPasses()
        {
            yield return null;
        }
    }
}
