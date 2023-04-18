namespace Shared;

public class Logger
{

    private const string FileName = @"C:\Users\FBR\RiderProjects\Handwerkerrechnungen\Handwerkerrechnung.log";

    public static void Log(string logMessage, LogLevel logLevel)
    {
        var level = logLevel switch
        {
            LogLevel.Warning => "Warning:",
            LogLevel.Error => "Error:",
            _ => "Information:"
        };

        LogMessage($"[{DateTime.Now} | {level} | {logMessage}");
    }

    public static void Info(string logMessage)
    {
        Log(logMessage, LogLevel.Info);
    }

    public static void Warning(string logMessage)
    {
        Log(logMessage, LogLevel.Warning);
    }

    public static void Error(string logMessage)
    {
        Log(logMessage, LogLevel.Error);
    }

    private static void LogMessage(string message)
    {
        try
        {
            if (!File.Exists(FileName))
            {
                File.Create(FileName).Dispose();
            }

            File.AppendAllText(FileName, message + Environment.NewLine);
        }
        catch
        {
            // ignored
        }
    }
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}