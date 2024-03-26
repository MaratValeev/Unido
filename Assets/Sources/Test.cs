using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unido
{
    public class Test : MonoBehaviour
    {
        [SerializeField] private TMP_InputField urlInputField;
        [SerializeField] private TMP_InputField filePathInputField;
        [SerializeField] private TextMeshProUGUI info;
        [SerializeField] private Button downloadButton;

        DownloadService downloader;
        DownloadOptions options;
        void Start()
        {
            options = new DownloadOptions();

            downloader = new DownloadService();
            downloadButton.onClick.AddListener(Download);
        }

        private void Download()
        {
            string url = urlInputField.text;
            string path = filePathInputField.text;
            options.Url = new Uri(url);
            options.FilePath = path;
            options.ProgressTriggerValue = 10;
            options.SpeedLimit = 1024 * 1000;
            options.ProgressTriggerType = DownloadProgressChangeTrigger.ByBufferCounts;

            var process = downloader.Download(options);
            process.DownloadEvent += (a) =>
            {
                info.text = a.ToString();
            };
        }

        private void OnApplicationQuit()
        {
            downloader.Dispose();
        }
    }
}
