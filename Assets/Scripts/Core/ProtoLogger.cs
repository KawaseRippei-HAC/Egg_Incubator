using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Proto.Core
{
    /// <summary>
    ///     統一されたログシステム
    ///     デバッグログの管理と出力を統一
    /// </summary>
    public static class ProtoLogger
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        private static LoggerSettings settings = new();

        public static void Configure(LoggerSettings newSettings)
        {
            settings = newSettings ?? new LoggerSettings();
        }

        public static void LogDebug(string message, Object context = null)
        {
            if (settings.enableDebugLogs)
            {
                var formattedMessage = FormatMessage("DEBUG", message);
                Debug.Log(formattedMessage, context);
            }
        }

        public static void LogInfo(string message, Object context = null)
        {
            if (settings.enableInfoLogs)
            {
                var formattedMessage = FormatMessage("INFO", message);
                Debug.Log(formattedMessage, context);
            }
        }

        public static void LogWarning(string message, Object context = null)
        {
            if (settings.enableWarningLogs)
            {
                var formattedMessage = FormatMessage("WARNING", message);
                Debug.LogWarning(formattedMessage, context);
            }
        }

        public static void LogError(string message, Object context = null)
        {
            if (settings.enableErrorLogs)
            {
                var formattedMessage = FormatMessage("ERROR", message);
                Debug.LogError(formattedMessage, context);
            }
        }

        public static void LogException(Exception exception, Object context = null)
        {
            if (settings.enableErrorLogs)
            {
                var formattedMessage = FormatMessage("EXCEPTION", exception.Message);
                Debug.LogException(exception, context);
            }
        }

        private static string FormatMessage(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var stackTrace = settings.enableStackTrace ? $"\nStackTrace: {Environment.StackTrace}" : "";
            return $"{settings.logPrefix} [{level}] [{timestamp}] {message}{stackTrace}";
        }

        public static void LogInput(string message, Object context = null)
        {
            LogDebug($"[Input] {message}", context);
        }

        public static void LogGesture(string message, Object context = null)
        {
            LogDebug($"[Gesture] {message}", context);
        }

        public static void LogUI(string message, Object context = null)
        {
            LogDebug($"[UI] {message}", context);
        }

        public static void LogSafeArea(string message, Object context = null)
        {
            LogDebug($"[SafeArea] {message}", context);
        }

        [Serializable]
        public class LoggerSettings
        {
            public bool enableDebugLogs = true;
            public bool enableInfoLogs = true;
            public bool enableWarningLogs = true;
            public bool enableErrorLogs = true;
            public bool enableStackTrace;
            public string logPrefix = "[Proto]";
        }
    }
}