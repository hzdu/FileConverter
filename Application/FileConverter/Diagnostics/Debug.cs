﻿// <copyright file="Debug.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;

    public static class Debug
    {
        private static string diagnosticsFolderPath;
        private static Dictionary<int, DiagnosticsData> diagnosticsDataById = new Dictionary<int, DiagnosticsData>();
        private static int threadCount = 0;
        private static int mainThreadId = 0;

        static Debug()
        {
            Debug.mainThreadId = Thread.CurrentThread.ManagedThreadId;

            string path = PathHelpers.GetUserDataFolderPath();

            // Delete old diagnostics folder (1 day).
            DateTime expirationDate = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            string[] diagnosticsDirectories = Directory.GetDirectories(path, "Diagnostics-*");
            for (int index = 0; index < diagnosticsDirectories.Length; index++)
            {
                string directory = diagnosticsDirectories[index];
                DateTime creationTime = Directory.GetCreationTime(directory);
                if (creationTime < expirationDate)
                {
                    Directory.Delete(directory, true);
                }
            }

            string diagnosticsFolderName = $"Diagnostics-{DateTime.Now.Hour}h{DateTime.Now.Minute}m{DateTime.Now.Second}s";
            
            Debug.diagnosticsFolderPath = Path.Combine(path, diagnosticsFolderName);
            Debug.diagnosticsFolderPath = PathHelpers.GenerateUniquePath(Debug.diagnosticsFolderPath);
            Directory.CreateDirectory(Debug.diagnosticsFolderPath);
        }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        public static DiagnosticsData[] Data
        {
            get
            {
                return Debug.diagnosticsDataById.Values.ToArray();
            }
        }

        public static void Log(string message, params object[] arguments)
        {
            string log = arguments.Length > 0 ? string.Format(message, arguments) : message;
            Debug.Log(log, ConsoleColor.White);
        }

        public static void Log(string message)
        {
            Debug.Log(message, ConsoleColor.White);
        }

        public static void Log(string log, ConsoleColor color)
        {
            DiagnosticsData diagnosticsData;
            
            Thread currentThread = Thread.CurrentThread;
            int threadId = currentThread.ManagedThreadId;

            // Display main thread logs in standard output.
            if (threadId == Debug.mainThreadId)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(log);
                Console.ResetColor();
            }

            lock (Debug.diagnosticsDataById)
            {
                if (!Debug.diagnosticsDataById.TryGetValue(threadId, out diagnosticsData))
                {
                    string threadName = Debug.threadCount > 0 ? $"{currentThread.Name} ({Debug.threadCount})" : "Application";
                    diagnosticsData = new DiagnosticsData(threadName);
                    diagnosticsData.Initialize(Debug.diagnosticsFolderPath, threadId);
                    Debug.diagnosticsDataById.Add(threadId, diagnosticsData);
                    Debug.threadCount++;

                    StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs("Data"));
                }
            }

            diagnosticsData.Log(log);
        }

        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                LogError(message);
            }
        }

        public static void LogError(string message, params object[] arguments)
        {
            string log = string.Format(message, arguments);

            MessageBox.Show(log, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Debug.Log($"Error: {log}", ConsoleColor.Red);
        }

        public static void LogError(int errorCode, string message)
        {
            Debug.LogError($"{message} (code 0x{errorCode:X})");
        }

        public static void Release()
        {
            Debug.Log("Diagnostics manager released correctly.");

            foreach (KeyValuePair<int, DiagnosticsData> kvp in Debug.diagnosticsDataById)
            {
                kvp.Value.Release();
            }

            Debug.diagnosticsDataById.Clear();
        }
    }
}
