using System;
using System.IO;
using BAModAPI;
using UnityEngine;

namespace AutoShopping
{
    internal static class ModLog
    {
        private const string Prefix = "[AutoShopping]";
        private const string LogFileName = "auto_shopping.log";
        private const string PerfLogFileName = "auto_shopping_perf.log";
        private const string LogsFolderName = "Logs";

        private static string _logsDir;

        internal static void Initialize(ModContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.ModRootPath))
                return;

            if (!AutoShoppingConfig.LogEnabled && !AutoShoppingConfig.LogPerf)
                return;

            _logsDir = Path.Combine(context.ModRootPath, LogsFolderName);
            try
            {
                Directory.CreateDirectory(_logsDir);
                if (AutoShoppingConfig.LogEnabled && AutoShoppingConfig.LogVerbose)
                    Info("Log file: " + Path.Combine(LogsFolderName, LogFileName));
            }
            catch (Exception ex)
            {
                if (AutoShoppingConfig.LogEnabled)
                    Debug.LogWarning(Prefix + " Failed to create Logs folder: " + ex.Message);
            }
        }

        internal static void Shutdown() => _logsDir = null;

        internal static void Boot(string message)
        {
            if (!AutoShoppingConfig.LogEnabled)
                return;

            Debug.Log(Prefix + " " + message);
        }

        internal static void Info(string message)
        {
            if (!AutoShoppingConfig.LogEnabled || !AutoShoppingConfig.LogVerbose)
                return;

            Debug.Log(Prefix + " " + message);
            WriteFile("INFO", message);
        }

        internal static void Warn(string message)
        {
            if (!AutoShoppingConfig.LogEnabled)
                return;

            Debug.LogWarning(Prefix + " " + message);
            WriteFile("WARN", message);
        }

        internal static void PerfSlow(string operation, double milliseconds, string detail = null)
        {
            if (!AutoShoppingConfig.LogPerf)
                return;

            var message = "SLOW " + operation + " " + milliseconds.ToString("F1") + "ms";
            if (!string.IsNullOrEmpty(detail))
                message += " | " + detail;

            if (AutoShoppingConfig.LogEnabled)
                Debug.LogWarning(Prefix + " [perf] " + message);

            WritePerfFile("SLOW", message);
        }

        internal static void PerfSummary(string summary)
        {
            if (!AutoShoppingConfig.LogPerf || string.IsNullOrEmpty(summary))
                return;

            WritePerfFile("SUMMARY", summary.TrimEnd());
        }

        private static void WriteFile(string level, string message)
        {
            if (!AutoShoppingConfig.LogEnabled || !AutoShoppingConfig.LogVerbose || string.IsNullOrEmpty(_logsDir))
                return;

            WriteToFile(LogFileName, level, message);
        }

        private static void WritePerfFile(string level, string message)
        {
            if (string.IsNullOrEmpty(_logsDir))
                return;

            WriteToFile(PerfLogFileName, level, message);
        }

        private static void WriteToFile(string fileName, string level, string message)
        {
            try
            {
                var path = Path.Combine(_logsDir, fileName);
                File.AppendAllText(
                    path,
                    DateTime.UtcNow.ToString("o") + " [" + level + "] " + message + Environment.NewLine);
            }
            catch
            {
                // ignore
            }
        }
    }
}
