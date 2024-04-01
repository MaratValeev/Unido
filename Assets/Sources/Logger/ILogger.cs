using System;
using UnityEngine;

namespace Unido
{
    public interface ILogger
    {
        public void Log(string message, LogType type = LogType.Log);
        public void Log(Exception exception) => Log(exception.Message, LogType.Exception);
        public string Format(string message, LogType type);
        public GameObject Context { get; set; }
    }
}
