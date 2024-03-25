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
        [SerializeField] private Button downloadButton;

        DownloadService downloader;

        // Start is called before the first frame update
        void Start()
        {
            downloader = new DownloadService();
            downloadButton.onClick.AddListener(Download);
        }

        private void Download()
        {
            string url = urlInputField.text;
            string path = filePathInputField.text;
            var process = downloader.DownloadAsFile(url, path);
        }
    }
}
