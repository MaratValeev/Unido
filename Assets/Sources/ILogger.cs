using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unido
{
    public interface ILogger
    {
        public void Log(string message, GameObject context = null, LogType type = LogType.Log);
        public string Format(string message);
        public GameObject Context { get; set; }
    }
}
