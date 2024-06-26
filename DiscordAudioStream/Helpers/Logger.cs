﻿using System.Diagnostics;

namespace DiscordAudioStream;

[System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S6670", Justification = "Trace.WriteLine in this class is intentional")]
internal static class Logger
{
    private const string LOG_FILE_PATH = "DiscordAudioStream_log.txt";

    private const int GROUP_TIME_DELTA_MS = 50;

    private static readonly int startTime = Environment.TickCount;

    private static string? groupCallerName;
    private static int groupLastLogTime;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S3963", Justification =
        "Cannot convert this static constructor into an inline initialization")]
    static Logger()
    {
        if (Properties.Settings.Default.OutputLogFile)
        {
            try
            {
                Stream file = File.Create(LOG_FILE_PATH);
                _ = Trace.Listeners.Add(new TextWriterTraceListener(file));
                Trace.AutoFlush = true;
            }
            catch (IOException)
            {
                Console.WriteLine($"Warning: The log file ({LOG_FILE_PATH}) is already in use.");
                Console.WriteLine("Logging will be disabled until the next launch.");
            }
        }
    }

    public static void EmptyLine()
    {
        ForceStartNewGroup();
        Trace.WriteLine("");
    }

    public static void Log(string text)
    {
        int timestamp = GetTimestamp();
        string callerName = GetCallerName();
        if (!GroupWithPreviousLogs(timestamp, callerName))
        {
            Trace.WriteLine($"\r\n{timestamp}ms [{callerName}]");
        }
        Trace.WriteLine("    " + SanitizeText(text));
        groupCallerName = callerName;
        groupLastLogTime = timestamp;
    }

    public static void Log(Exception e)
    {
        string separator = new('-', 40);
        Log($"Exception:\n{separator}\n{e}\n{separator}");
    }

    private static string GetCallerName()
    {
        StackTrace stackTrace = new();
        // Go down the stack until a method that isn't in this class is found
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            System.Reflection.MethodBase method = frame.GetMethod();
            if (method.DeclaringType != typeof(Logger))
            {
                return $"{method.DeclaringType.Name}.{method.Name}";
            }
        }
        return "Unknown";
    }

    private static int GetTimestamp()
    {
        return Environment.TickCount - startTime;
    }

    private static bool GroupWithPreviousLogs(int timestamp, string callerName)
    {
        bool longTimeSinceLastLog = timestamp - groupLastLogTime > GROUP_TIME_DELTA_MS;
        bool sameCaller = callerName == groupCallerName;
        return !longTimeSinceLastLog && sameCaller;
    }

    private static void ForceStartNewGroup()
    {
        groupCallerName = null;
    }

    private static string SanitizeText(string text)
    {
        // CRLF required for Windows 7 Notepad
        string normalizedLineEnding = text.Replace("\r\n", "\n");
        return normalizedLineEnding.Replace("\n", "\r\n    ");
    }
}
