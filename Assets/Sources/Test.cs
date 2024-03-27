using System;
using System.IO;
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

        private DownloadService downloader;
        private DownloadOptions options;
        private DownloadProcess process;

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
            options.ProgressTriggerValue = 0.01F;
            options.BufferSize = 4096 * 4;
            options.ProgressTriggerType = DownloadProgressChangeTrigger.ByPrecentage;
            options.DeleteOnCancelOrOnFail = false;

            if (File.Exists(path))
            {
                options.FileCreationMode = FileCreationMode.TryContinue;
            }
            else
            {
                options.FileCreationMode = FileCreationMode.Replace;
            }

            process = downloader.Download(options);
            if (process == null)
            {
                return;
            }

            process.DownloadEvent += (a) =>
            {
                info.text = a.ToString();
            };
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (process != null)
                {
                    process.State.Paused = !process.State.Paused;
                }
            }
        }

        private void OnApplicationQuit()
        {
            downloader?.Dispose();
        }
    }
}
