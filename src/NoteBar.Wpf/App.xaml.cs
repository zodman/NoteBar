using System;
using System.IO;
using System.Windows;

namespace NoteBar.Wpf
{
    public partial class App : Application
    {
        private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteBar", "app.log");

        public static void Log(string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log("App.OnStartup called with args: " + string.Join(" ", e.Args));
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Log($"AppDomain UnhandledException: {args.ExceptionObject}");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Log($"DispatcherUnhandledException: {args.Exception}");
                args.Handled = true;
            };

            base.OnStartup(e);

            uint? initialPort = null;
            for (int i = 0; i < e.Args.Length; i++)
            {
                if ((e.Args[i] == "-p" || e.Args[i] == "--port") && i + 1 < e.Args.Length)
                {
                    if (uint.TryParse(e.Args[i + 1], out var p)) initialPort = p;
                }
                else if (uint.TryParse(e.Args[i], out var p))
                {
                    initialPort = p;
                }
            }

            var mainWindow = new MainWindow(initialPort);
            MainWindow = mainWindow;
            mainWindow.Show();
            Log("MainWindow shown");
        }
    }
}
