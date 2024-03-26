using System;
using UnityEngine;

namespace Unido
{
    public class UnidoLogger : ILogger
    {
        public GameObject Context { get; set; }

        public UnidoLogger(GameObject context = null)
        {
            Context = context;
        }

        public string Format(string message)
        {
            return $"[{DateTime.Now}] {nameof(UnidoLogger)}: {message}";
        }

        public void Log(string message, GameObject context = null, LogType type = LogType.Log)
        {
            string formatted = Format(message);

            switch (type)
            {
                case LogType.Exception:
                case LogType.Error:
                    Debug.LogError(formatted, context); break;

                case LogType.Warning:
                    Debug.LogWarning(formatted, context);
                    break;

                default:
                    Debug.Log(formatted, context);
                    break;
            }
        }
    }
}
