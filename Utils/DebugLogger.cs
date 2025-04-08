using UnityEngine;

namespace PaintedUtils
{
    public class DebugLogger : MonoBehaviour
    {
        public void PrintToLog(string message)
        {
            Debug.Log($"[{gameObject.name}] {message}");
        }
    }
} 