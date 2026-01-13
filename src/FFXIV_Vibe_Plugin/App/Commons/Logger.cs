using NoireLib;
using System;

namespace FFXIV_Vibe_Plugin.Commons;

public static class Logger
{
    private static readonly string Name = "";
    private static readonly LogLevelEnum LogLevel = LogLevelEnum.DEBUG;
    private static readonly string Prefix = ">";

    public static void Chat(string msg)
    {
        NoireLogger.PrintToChat(FormatMessage(LogLevelEnum.LOG, msg));
    }

    public static void ChatError(string msg)
    {
        NoireService.ChatGui.PrintError(FormatMessage(LogLevelEnum.ERROR, msg));
        Error(msg);
    }

    public static void ChatError(string msg, Exception e)
    {
        string msg1 = FormatMessage(LogLevelEnum.ERROR, msg, e);
        NoireService.ChatGui.PrintError(msg1);
        Error(msg1);
    }

    public static void Verbose(string msg)
    {
        if (LogLevel > LogLevelEnum.VERBOSE)
            return;
        NoireLogger.LogVerbose(FormatMessage(LogLevelEnum.VERBOSE, msg));
    }

    public static void Debug(string msg)
    {
        if (LogLevel > LogLevelEnum.DEBUG)
            return;
        NoireLogger.LogDebug(FormatMessage(LogLevelEnum.DEBUG, msg));
    }

    public static void Log(string msg)
    {
        if (LogLevel > LogLevelEnum.LOG)
            return;
        NoireLogger.LogDebug(FormatMessage(LogLevelEnum.LOG, msg));
    }

    public static void Info(string msg)
    {
        if (LogLevel > LogLevelEnum.INFO)
            return;
        NoireLogger.LogInfo(FormatMessage(LogLevelEnum.INFO, msg));
    }

    public static void Warn(string msg)
    {
        if (LogLevel > LogLevelEnum.WARN)
            return;
        NoireLogger.LogWarning(FormatMessage(LogLevelEnum.WARN, msg));
    }

    public static void Error(string msg)
    {
        if (LogLevel > LogLevelEnum.ERROR)
            return;
        NoireLogger.LogError(FormatMessage(LogLevelEnum.ERROR, msg));
    }

    public static void Error(string msg, Exception e)
    {
        if (LogLevel > LogLevelEnum.ERROR)
            return;
        NoireLogger.LogError(FormatMessage(LogLevelEnum.ERROR, msg, e));
    }

    public static void Fatal(string msg)
    {
        if (LogLevel > LogLevelEnum.FATAL)
            return;
        NoireLogger.LogFatal(FormatMessage(LogLevelEnum.FATAL, msg));
    }

    public static void Fatal(string msg, Exception e)
    {
        if (LogLevel > LogLevelEnum.FATAL)
            return;
        NoireLogger.LogFatal(FormatMessage(LogLevelEnum.FATAL, msg, e));
    }

    private static string FormatMessage(LogLevelEnum type, string msg) => $"{(Name != "" ? Name + " " : "")}{type} {Prefix} {msg}";

    private static string FormatMessage(LogLevelEnum type, string msg, Exception e) => $"{(Name != "" ? Name + " " : "")}{type} {Prefix} {e.Message}\\n{msg}";

    public enum LogLevelEnum
    {
        VERBOSE,
        DEBUG,
        LOG,
        INFO,
        WARN,
        ERROR,
        FATAL,
    }
}
