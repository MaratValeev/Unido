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
            var config = new DownloadServiceConfig();
            downloader = new DownloadService(config);
            downloadButton.onClick.AddListener(Download);
        }

        private void Download()
        {
            string url = urlInputField.text;
            string path = filePathInputField.text;
            options.Url = new Uri(url);
            options.FilePath = path;
            options.ProgressTriggerValue = 0.5F;
            options.SpeedLimit = 0;
            options.BufferSize = 4096 * 4;
            options.ProgressTriggerType = DownloadProgressChangeTrigger.ByPrecentage;

            var process = downloader.Download(options);
            if (process == null)
            {
                return;
            }

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
