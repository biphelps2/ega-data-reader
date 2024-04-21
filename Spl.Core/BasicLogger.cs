using System;
using System.IO;

namespace Spl.Core
{
    public static class BasicLogger
    {
        public static string? LoggerName;
        public static string LogFilePathError => $" {DateTime.Now:yyyyMMdd}-{LoggerName ?? "logger"}-error.log";
        public static string LogFilePathInfo => $" {DateTime.Now:yyyyMMdd}-{LoggerName ?? "logger"}-log.log";

        public static void SetName(string name)
        {
            LoggerName = name;
        }

        public static void LogDetail(string str)
        {
            var logText = $"{DateTime.Now} Detail: {str}";

#if CODEGEN_WASM
            Console.WriteLine(logText);
#else
            try
            {
                // No console output.
                File.AppendAllText(LogFilePathInfo, logText + "\n");
            }
            catch
            {
                // Our logger should not cause additional problems. Not great that we throw away
                // the error here but better than stack overflow and crash.
            }
#endif
        }

        public static void LogInfo(string str)
        {
            var logText = $"{DateTime.Now} Info: {str}";

#if CODEGEN_WASM
            Console.WriteLine(logText);
#else
            try
            {
                var consoleColour = Console.ForegroundColor;
                var consoleColourBg = Console.BackgroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(logText);
                Console.ForegroundColor = consoleColour;
                Console.BackgroundColor = consoleColourBg;
                Console.WriteLine();

                File.AppendAllText(LogFilePathInfo, logText + "\n");
            }
            catch
            {
                // Our logger should not cause additional problems. Not great that we throw away
                // the error here but better than stack overflow and crash.
            }
#endif
        }

        public static void LogWarning(string str)
        {
            var logText = $"{DateTime.Now} Warning: {str}";

#if CODEGEN_WASM
            Console.WriteLine(logText);
#else
            try
            {
                var consoleColour = Console.ForegroundColor;
                var consoleColourBg = Console.BackgroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(logText);
                Console.ForegroundColor = consoleColour;
                Console.BackgroundColor = consoleColourBg;
                Console.WriteLine();

                File.AppendAllText(LogFilePathInfo, logText + "\n");
            }
            catch
            {
                // Our logger should not cause additional problems. Not great that we throw away
                // the error here but better than stack overflow and crash.
            }
#endif
        }

        public static void LogError(string str)
        {
            var logText = $"{DateTime.Now} Error: {str}";

#if CODEGEN_WASM
            Console.WriteLine(logText);
#else
            try
            {
                var consoleColour = Console.ForegroundColor;
                var consoleColourBg = Console.BackgroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(logText);
                Console.ForegroundColor = consoleColour;
                Console.BackgroundColor = consoleColourBg;
                Console.WriteLine();

                File.AppendAllText(LogFilePathError, logText + "\n");
            }
            catch
            {
                // Our logger should not cause additional problems. Not great that we throw away
                // the error here but better than stack overflow and crash.
            }
#endif
        }
    }
}
