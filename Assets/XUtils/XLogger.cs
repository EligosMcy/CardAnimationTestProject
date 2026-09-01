
using ShowX.Utils.Entity.Enum;
using System;
using UnityEngine;

namespace ShowX.Utils
{
    /**
     * Use XLogger to print several level log.
     * XLoggerLevel includes several logger levels, such as Trace, Debug, Info, Warning, Assert, Error, Fatal.
     */
    public class XLogger : Debug
    {
        private static XLoggerLevel _loggerLevel = XLoggerLevel.Info;
        private static bool _levelColorEnabled = true;

        //setting
        public static void EnableLogger(bool enable)
        {
            unityLogger.logEnabled = enable;
        }

        public static void EnableLevelColor(bool enable)
        {
            _levelColorEnabled = enable;
        }

        public static void FilterLoggerLevel(XLoggerLevel loggerLevel = XLoggerLevel.Info)
        {
            //
            unityLogger.logEnabled = true;

            //
            _loggerLevel = loggerLevel;

            switch (_loggerLevel)
            {
                case XLoggerLevel.Trace:

                case XLoggerLevel.Debug:

                case XLoggerLevel.Info:
                    unityLogger.filterLogType = LogType.Log;
                    break;
                case XLoggerLevel.Warning:
                    unityLogger.filterLogType = LogType.Warning;
                    break;
                case XLoggerLevel.Assert:
                    unityLogger.filterLogType = LogType.Assert;
                    break;
                case XLoggerLevel.Error:
                    unityLogger.filterLogType = LogType.Error;
                    break;
                case XLoggerLevel.Fatal:
                    unityLogger.filterLogType = LogType.Exception;
                    break;
                //
                default:
                    unityLogger.filterLogType = LogType.Error;
                    break;
            }

        }

        //check
        public static bool IsTraceEnabled()
        {
            return unityLogger.IsLogTypeAllowed(LogType.Log) && _loggerLevel <= XLoggerLevel.Trace;
        }

        public static bool IsDebugEnabled()
        {
            return unityLogger.IsLogTypeAllowed(LogType.Log) && _loggerLevel <= XLoggerLevel.Debug;
        }

        public static bool IsInfoEnabled()
        {
            return unityLogger.IsLogTypeAllowed(LogType.Log) && _loggerLevel <= XLoggerLevel.Info;
        }

        public static bool IsWarningEnabled()
        {
            return unityLogger.IsLogTypeAllowed(LogType.Warning);
        }

        public static bool IsAssetEnabled()
        {
            return unityLogger.IsLogTypeAllowed(LogType.Assert);
        }

        //log
        public static void LogTrace(string tag, object message)
        {
            if (IsTraceEnabled())
            {
                if (_levelColorEnabled)
                {
                    LogFormat("<color={0}>[TRACE] [{1}] {2}</color>", getLevelMappedColor(XLoggerLevel.Trace), tag, message);
                }
                else
                {
                    LogFormat("[TRACE] [{0}] {1}", tag, message);
                }
            }
        }

        public static void LogDebug(string tag, object message)
        {
            if (IsDebugEnabled())
            {
                if (_levelColorEnabled)
                {
                    LogFormat("<color={0}>[DEBUG] [{1}] {2}</color>", getLevelMappedColor(XLoggerLevel.Debug), tag, message);
                }
                else
                {
                    LogFormat("[DEBUG] [{0}] {1}", tag, message);
                }
            }
        }

        public static void LogInfo(string tag, object message)
        {
            if (IsInfoEnabled())
            {
                if (_levelColorEnabled)
                {
                    LogFormat("<color={0}>[INFO] [{1}] {2}</color>", getLevelMappedColor(XLoggerLevel.Info), tag, message);
                }
                else
                {
                    LogFormat("[INFO] [{0}] {1}", tag, message);
                }
            }
        }

        public static void LogWarning(string tag, object message)
        {
            if (IsWarningEnabled())
            {
                if (_levelColorEnabled)
                {
                    LogWarningFormat("<color={0}>[WARNING] [{1}] {2}</color>", getLevelMappedColor(XLoggerLevel.Warning), tag, message);
                }
                else
                {
                    LogWarningFormat("[WARNING] [{0}] {1}", tag, message);
                }
            }
        }

        public static void LogAssert(string tag, object message)
        {
            if (IsAssetEnabled())
            {
                if (_levelColorEnabled)
                {
                    LogAssertionFormat("<color={0}>[ASSERT] [{1}] {2}</color>", getLevelMappedColor(XLoggerLevel.Assert), tag, message);
                }
                else
                {
                    LogAssertionFormat("[ASSERT] [{0}] {1}", tag, message);
                }
            }
        }

        public static void LogError(string tag, object message)
        {
            if (_levelColorEnabled)
            {
                LogErrorFormat("<color={0}>[ERROR] [{1}] {2}</color>", getLevelMappedColor(XLoggerLevel.Error), tag, message);
            }
            else
            {
                LogErrorFormat("[ERROR] [{0}] {1}", tag, message);
            }
        }

        public static void LogFatal(Exception exception)
        {
            LogException(exception);
        }

        //
        private static string getLevelMappedColor(XLoggerLevel loggerLevel)
        {
            string returnValue = null;

            switch (loggerLevel)
            {
                case XLoggerLevel.Trace:
                    returnValue = "#A0A0A0";
                    break;
                case XLoggerLevel.Debug:
                    returnValue = "#A0A0A0";
                    break;
                case XLoggerLevel.Info:
                    returnValue = "green";
                    break;
                case XLoggerLevel.Warning:
                    returnValue = "yellow";
                    break;
                case XLoggerLevel.Assert:
                    returnValue = "purple";
                    break;
                case XLoggerLevel.Error:
                    returnValue = "red";
                    break;
                case XLoggerLevel.Fatal:
                    returnValue = "red";
                    break;
                //
                default:
                    break;
            }

            return returnValue;
        }
    }
}

