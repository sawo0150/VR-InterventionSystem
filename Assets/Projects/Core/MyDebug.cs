using UnityEngine;
using System.Diagnostics;

namespace Project
{
    public static class MyDebug
    {
        private static bool debuggingFlag = false;
        
        public static void SetDebuggingFlag(bool flag)
        {
            debuggingFlag = flag;
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            if (!debuggingFlag) return;
            
            UnityEngine.Debug.Log($"[Log] {message}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message)
        {
            if (!debuggingFlag) return;
            
            UnityEngine.Debug.LogWarning($"[Warn] {message}");
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message)
        {
            if (!debuggingFlag) return;
            
            UnityEngine.Debug.LogError($"[Warn] {message}");
        }
    }
}
