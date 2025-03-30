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
        // PluginLog.LogVerbose(this.FormatMessage(Logger.LogLevel.VERBOSE, msg), Array.Empty<object>());
    }

    public static void Debug(string msg)
    {
        if (log_level > Logger.LogLevel.DEBUG)
            return;
        // PluginLog.LogDebug(this.FormatMessage(Logger.LogLevel.DEBUG, msg), Array.Empty<object>());
    }

    public static void Log(string msg)
    {
        if (log_level > Logger.LogLevel.LOG)
            return;
        // PluginLog.Log(this.FormatMessage(Logger.LogLevel.LOG, msg), Array.Empty<object>());
    }

    public static void Info(string msg)
    {
        if (log_level > Logger.LogLevel.INFO)
            return;
        // PluginLog.Information(this.FormatMessage(Logger.LogLevel.INFO, msg), Array.Empty<object>());
    }

    public static void Warn(string msg)
    {
        if (log_level > Logger.LogLevel.WARN)
            return;
        // PluginLog.Warning(this.FormatMessage(Logger.LogLevel.WARN, msg), Array.Empty<object>());
    }

    public static void Error(string msg)
    {
        if (log_level > Logger.LogLevel.ERROR)
            return;
        // PluginLog.Error(this.FormatMessage(Logger.LogLevel.ERROR, msg), Array.Empty<object>());
    }

    public static void Error(string msg, Exception e)
    {
        if (log_level > Logger.LogLevel.ERROR)
            return;
        // PluginLog.Error(this.FormatMessage(Logger.LogLevel.ERROR, msg, e), Array.Empty<object>());
    }

    public static void Fatal(string msg)
    {
        if (log_level > Logger.LogLevel.FATAL)
            return;
        // PluginLog.Fatal(this.FormatMessage(Logger.LogLevel.FATAL, msg), Array.Empty<object>());
    }

    public static void Fatal(string msg, Exception e)
    {
        if (log_level > Logger.LogLevel.FATAL)
            return;
        // PluginLog.Fatal(this.FormatMessage(Logger.LogLevel.FATAL, msg, e), Array.Empty<object>());
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
