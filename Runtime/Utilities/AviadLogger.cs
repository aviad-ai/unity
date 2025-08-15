using UnityEngine;
using System.Diagnostics;

namespace Aviad
{
    /// <summary>
    /// Package-level logging class that respects AviadGlobalSettings for log filtering
    /// </summary>
    public static class AviadLogger
    {
        private const string LOG_PREFIX = "[Aviad]";

        /// <summary>
        /// Log a verbose message (lowest priority)
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional Unity object context</param>
        public static void Verbose(string message, Object context = null)
        {
            if (ShouldLog(LogLevel.Verbose))
            {
                UnityEngine.Debug.Log($"{LOG_PREFIX} [VERBOSE] {message}", context);
            }
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional Unity object context</param>
        public static void Debug(string message, Object context = null)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                UnityEngine.Debug.Log($"{LOG_PREFIX} [DEBUG] {message}", context);
            }
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional Unity object context</param>
        public static void Info(string message, Object context = null)
        {
            if (ShouldLog(LogLevel.Info))
            {
                UnityEngine.Debug.Log($"{LOG_PREFIX} [INFO] {message}", context);
            }
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional Unity object context</param>
        public static void Warning(string message, Object context = null)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                UnityEngine.Debug.LogWarning($"{LOG_PREFIX} [WARNING] {message}", context);
            }
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional Unity object context</param>
        public static void Error(string message, Object context = null)
        {
            if (ShouldLog(LogLevel.Error))
            {
                UnityEngine.Debug.LogError($"{LOG_PREFIX} [ERROR] {message}", context);
            }
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">Optional Unity object context</param>
        public static void Exception(System.Exception exception, Object context = null)
        {
            if (ShouldLog(LogLevel.Error))
            {
                UnityEngine.Debug.LogException(exception, context);
            }
        }

        /// <summary>
        /// Check if a log level should be logged based on current settings
        /// </summary>
        /// <param name="level">The log level to check</param>
        /// <returns>True if the message should be logged</returns>
        private static bool ShouldLog(LogLevel level)
        {
            // If log level is None, don't log anything
            var currentLogLevel = AviadGlobalSettings.CurrentLogLevel;
            if (currentLogLevel == LogLevel.None)
                return false;

            // Only log if the message level is >= current log level
            return level >= currentLogLevel;
        }

        /// <summary>
        /// Check if a specific log level is enabled
        /// </summary>
        /// <param name="level">The log level to check</param>
        /// <returns>True if logging is enabled for this level</returns>
        public static bool IsLogLevelEnabled(LogLevel level)
        {
            return ShouldLog(level);
        }

        /// <summary>
        /// Get the current effective log level
        /// </summary>
        /// <returns>The current log level from settings</returns>
        public static LogLevel GetCurrentLogLevel()
        {
            return AviadGlobalSettings.CurrentLogLevel;
        }

        /// <summary>
        /// Check if native logging is currently enabled
        /// </summary>
        /// <returns>True if native logging is enabled</returns>
        public static bool IsNativeLoggingEnabled()
        {
            return AviadGlobalSettings.IsNativeLoggingEnabled;
        }

        #region Formatted Logging Methods

        /// <summary>
        /// Log a formatted verbose message
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void VerboseFormat(string format, params object[] args)
        {
            if (ShouldLog(LogLevel.Verbose))
            {
                UnityEngine.Debug.LogFormat($"{LOG_PREFIX} [VERBOSE] {format}", args);
            }
        }

        /// <summary>
        /// Log a formatted debug message
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void DebugFormat(string format, params object[] args)
        {
            if (ShouldLog(LogLevel.Debug))
            {
                UnityEngine.Debug.LogFormat($"{LOG_PREFIX} [DEBUG] {format}", args);
            }
        }

        /// <summary>
        /// Log a formatted info message
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void InfoFormat(string format, params object[] args)
        {
            if (ShouldLog(LogLevel.Info))
            {
                UnityEngine.Debug.LogFormat($"{LOG_PREFIX} [INFO] {format}", args);
            }
        }

        /// <summary>
        /// Log a formatted warning message
        /// </summary>
        public static void WarningFormat(string format, params object[] args)
        {
            if (ShouldLog(LogLevel.Warning))
            {
                UnityEngine.Debug.LogWarningFormat($"{LOG_PREFIX} [WARNING] {format}", args);
            }
        }

        /// <summary>
        /// Log a formatted error message
        /// </summary>
        public static void ErrorFormat(string format, params object[] args)
        {
            if (ShouldLog(LogLevel.Error))
            {
                UnityEngine.Debug.LogErrorFormat($"{LOG_PREFIX} [ERROR] {format}", args);
            }
        }

        #endregion
    }
}