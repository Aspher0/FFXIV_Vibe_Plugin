using FFXIV_Vibe_Plugin.App;
using System;

namespace FFXIV_Vibe_Plugin.Commons;

public static class Logger
{
    private static readonly string name = "";
    private static readonly Logger.LogLevel log_level = Logger.LogLevel.DEBUG;
    private static readonly string prefix = ">";

    public static void Chat(string msg)
    {
        Service.DalamudChat.Print(FormatMessage(Logger.LogLevel.LOG, msg), null, new ushort?());
    }

    public static void ChatError(string msg)
    {
        Service.DalamudChat.PrintError(FormatMessage(Logger.LogLevel.ERROR, msg), null, new ushort?());
        Error(msg);
    }

    public static void ChatError(string msg, Exception e)
    {
        string msg1 = FormatMessage(Logger.LogLevel.ERROR, msg, e);
        Service.DalamudChat.PrintError(msg1, null, new ushort?());
        Error(msg1);
    }

    public static void Verbose(string msg)
    {
        if (log_level > Logger.LogLevel.VERBOSE)
            return;
        Service.PluginLog.Verbose(FormatMessage(Logger.LogLevel.VERBOSE, msg));
    }

    public static void Debug(string msg)
    {
        if (log_level > Logger.LogLevel.DEBUG)
            return;
        Service.PluginLog.Debug(FormatMessage(Logger.LogLevel.DEBUG, msg));
    }

    public static void Log(string msg)
    {
        if (log_level > Logger.LogLevel.LOG)
            return;
        Service.PluginLog.Debug(FormatMessage(Logger.LogLevel.LOG, msg));
    }

    public static void Info(string msg)
    {
        if (log_level > Logger.LogLevel.INFO)
            return;
        Service.PluginLog.Information(FormatMessage(Logger.LogLevel.INFO, msg));
    }

    public static void Warn(string msg)
    {
        if (log_level > Logger.LogLevel.WARN)
            return;
        Service.PluginLog.Warning(FormatMessage(Logger.LogLevel.WARN, msg));
    }

    public static void Error(string msg)
    {
        if (log_level > Logger.LogLevel.ERROR)
            return;
        Service.PluginLog.Error(FormatMessage(Logger.LogLevel.ERROR, msg));
    }

    public static void Error(string msg, Exception e)
    {
        if (log_level > Logger.LogLevel.ERROR)
            return;
        Service.PluginLog.Error(FormatMessage(Logger.LogLevel.ERROR, msg, e));
    }

    public static void Fatal(string msg)
    {
        if (log_level > Logger.LogLevel.FATAL)
            return;
        Service.PluginLog.Fatal(FormatMessage(Logger.LogLevel.FATAL, msg));
    }

    public static void Fatal(string msg, Exception e)
    {
        if (log_level > Logger.LogLevel.FATAL)
            return;
        Service.PluginLog.Fatal(FormatMessage(LogLevel.FATAL, msg, e));
    }

    private static string FormatMessage(Logger.LogLevel type, string msg) => $"{(name != "" ? name + " " : "")}{type} {prefix} {msg}";

    private static string FormatMessage(Logger.LogLevel type, string msg, Exception e) => $"{(name != "" ? name + " " : "")}{type} {prefix} {e.Message}\\n{msg}";

    public enum LogLevel
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
