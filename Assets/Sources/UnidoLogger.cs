using UnityEngine;

namespace Unido
{
    public class UnidoLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }
    }
}
