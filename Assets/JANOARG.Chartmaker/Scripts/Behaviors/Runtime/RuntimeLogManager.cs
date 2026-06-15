using UnityEngine;
using System.Collections.Generic;

namespace JANOARG.Chartmaker.Behaviors.Runtime {
    public static class RuntimeLogManager
    {
        public static readonly List<LoggerEntry> Logger = new();

        public class LoggerEntry
        {
            public string Message;
            public string StackTrace;
            public LogType LogType;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            Application.logMessageReceived -= RegisterLog;
            Application.logMessageReceived += RegisterLog;
        }

        static void RegisterLog(string condition, string stackTrace, LogType type)
        {
            Logger.Add(new LoggerEntry
            {
                Message = condition,
                StackTrace = stackTrace,
                LogType = type,
            });
        }
    }
}
