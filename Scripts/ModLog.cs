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
        private const string LogsFolderName = "Logs";

        private static string _logsDir;

        internal static void Initialize(ModContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.ModRootPath))
                return;

            _logsDir = Path.Combine(context.ModRootPath, LogsFolderName);
            try
            {
                Directory.CreateDirectory(_logsDir);
                Info("Log file: " + Path.Combine(LogsFolderName, LogFileName));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Prefix + " Failed to create Logs folder: " + ex.Message);
            }
        }

        internal static void Shutdown() => _logsDir = null;

        internal static void Info(string message)
        {
            Debug.Log(Prefix + " " + message);
            WriteFile("INFO", message);
        }

        internal static void Warn(string message)
        {
            Debug.LogWarning(Prefix + " " + message);
            WriteFile("WARN", message);
        }

        private static void WriteFile(string level, string message)
        {
            if (string.IsNullOrEmpty(_logsDir))
                return;

            try
            {
                var path = Path.Combine(_logsDir, LogFileName);
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
