using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using NoteBar.Core.Ipc;

namespace NoteBar.App
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    async options => await ExecuteAsync(options),
                    errors => Task.FromResult(1));
        }

        private static async Task<int> ExecuteAsync(Options options)
        {
            if (options.Exit)
            {
                try
                {
                    await NoteBarIpcClient.SendCommandAsync("QUIT", 2000);
                    Console.WriteLine("NoteBar stopped.");
                }
                catch
                {
                    Console.WriteLine("NoteBar is not running.");
                }
                return 0;
            }

            if (options.Quit)
            {
                try
                {
                    await NoteBarIpcClient.SendCommandAsync($"REMOVE:{options.Port}", 2000);
                    Console.WriteLine($"Indicator {options.Port} removed.");
                }
                catch
                {
                    Console.WriteLine("NoteBar is not running.");
                }
                return 0;
            }

            Console.WriteLine("Adding indicator...");

            // Try sending ADD to already-running host
            try
            {
                var response = await NoteBarIpcClient.SendCommandAsync($"ADD:{options.Port}", 1500);
                if (response == "OK" || response == "ERROR:Already started")
                {
                    Console.WriteLine("Indicator was added");
                    return 0;
                }
                else
                {
                    var error = response.StartsWith("ERROR:") ? response.Substring(6).Trim() : response;
                    Console.WriteLine($"Cannot add indicator. Error: {error}");
                    return 1;
                }
            }
            catch
            {
                // Host not running, launch it
                if (!StartHostProcess(options.Port))
                {
                    Console.WriteLine("Cannot add indicator. Error: NoteBar host executable not found.");
                    return 1;
                }

                Console.WriteLine("Indicator was added");
                return 0;
            }
        }

        private static bool StartHostProcess(uint? port = null)
        {
            var hostPath = FindHostExecutable();
            if (hostPath == null)
                return false;

            var args = port.HasValue ? $"--port {port.Value}" : "";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = hostPath,
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(hostPath)
                };
                Process.Start(startInfo);
                return true;
            }
            catch
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = hostPath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        WorkingDirectory = Path.GetDirectoryName(hostPath)
                    };

                    var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
                    if (!string.IsNullOrEmpty(dotnetRoot))
                    {
                        startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetRoot;
                    }

                    Process.Start(startInfo);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start host: {ex.Message}");
                    return false;
                }
            }
        }

        private static string FindHostExecutable()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Same directory (installed / published)
            var sameDir = Path.Combine(baseDir, "NoteBar.Wpf.exe");
            if (File.Exists(sameDir)) return sameDir;

            // 2. Walk up directory tree to find NoteBar.Wpf output
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var checkDev = Path.Combine(dir.FullName, "NoteBar.Wpf", "bin", "Debug", "net8.0-windows", "NoteBar.Wpf.exe");
                if (File.Exists(checkDev)) return Path.GetFullPath(checkDev);

                var checkRelease = Path.Combine(dir.FullName, "NoteBar.Wpf", "bin", "Release", "net8.0-windows", "NoteBar.Wpf.exe");
                if (File.Exists(checkRelease)) return Path.GetFullPath(checkRelease);

                var checkDirect = Path.Combine(dir.FullName, "NoteBar.Wpf.exe");
                if (File.Exists(checkDirect)) return Path.GetFullPath(checkDirect);

                dir = dir.Parent;
            }

            return null;
        }
    }
}