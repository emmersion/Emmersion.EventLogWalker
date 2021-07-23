using System;
using System.Diagnostics;
using System.IO;

namespace ExampleReports
{
    public class TimeTracker
    {
        private readonly int reportRemainingTimeAfterCompleting;
        private readonly Stopwatch stopwatch;
        private int completed;

        public TimeTracker(int reportRemainingTimeAfterCompleting = 10)
        {
            this.reportRemainingTimeAfterCompleting = reportRemainingTimeAfterCompleting;
            stopwatch = Stopwatch.StartNew();
        }

        public void ItemCompleted(string customMessagePrefix = null)
        {
            if ( ++completed % reportRemainingTimeAfterCompleting != 0 ) return;

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var averageMs = elapsedMs / (double) completed;
            ConsoleLogger.Info($"{customMessagePrefix}Completed {completed}. Duration: {TimeSpan.FromMilliseconds(elapsedMs)}. Avg/item: {TimeSpan.FromMilliseconds(averageMs)}.");
        }
    }

    public static class ConsoleLogger
    {
        private static string Filename;
        public static void EchoToFile(string filename)
        {
            Filename = filename;
            Console.WriteLine($"[{DateTimeOffset.Now:O}] {"Logs echoed to: " + Path.GetFullPath(Filename)}");
        }
        public static void Danger(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Info(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void Caution(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Info(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Info(string message)
        {
            var logMessage = $"[{DateTimeOffset.Now:O}] {message}";
            Console.WriteLine(logMessage);
            if (!string.IsNullOrEmpty(Filename))
            {
                File.AppendAllText(Filename, $"{logMessage}{Environment.NewLine}");
            }
        }

        public static void LogToFileOnly(string message)
        {
            var logMessage = $"[{DateTimeOffset.Now:O}] {message}";
            if (!string.IsNullOrEmpty(Filename))
            {
                File.AppendAllText(Filename, $"{logMessage}{Environment.NewLine}");
            }
        }


        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Info(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Callout(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Info(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
