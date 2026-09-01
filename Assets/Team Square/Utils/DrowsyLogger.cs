using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utils
{
    public static class DrowsyLogger
    {
        private static void DoLog(Action<string, Object> LogFunction, Object myObj, string preFix, object msg)
        {
            LogFunction($"{preFix} : {msg}", myObj);
        }


        public static void Log(this Object myObj, object msg)
        {
           DoLog(Debug.Log, myObj, $"<color=lightBlue>[{myObj}]</color>", msg);
        }
        
        public static void LogWarning(this Object myObj, object msg)
        {
           DoLog(Debug.LogWarning, myObj, $"<color=yellow>[{myObj}]</color>", msg);
        }
          
        public static void LogError(this Object myObj, object msg)
        {
           DoLog(Debug.LogError, myObj, $"<color=red>[{myObj}]</color>", msg);
        }
    }
}