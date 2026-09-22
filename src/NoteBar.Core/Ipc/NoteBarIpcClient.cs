using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoteBar.Core.Ipc
{
    public static class NoteBarIpcClient
    {
        public static Action<string> LogAction { get; set; }

        public static async Task<bool> IsServerRunningAsync(int timeoutMs = 1500)
        {
            try
            {
                var response = await SendCommandAsync("PING", timeoutMs).ConfigureAwait(false);
                return response == "PONG";
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[IpcClient] IsServerRunning check: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> SendCommandAsync(string command, int timeoutMs = 2500)
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            LogAction?.Invoke($"[IpcClient] Connecting to pipe {NoteBarIpcConstants.PipeName} (timeout {timeoutMs}ms)...");

            using var client = new NamedPipeClientStream(".", NoteBarIpcConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(cts.Token).ConfigureAwait(false);
            LogAction?.Invoke("[IpcClient] Connected!");

            LogAction?.Invoke($"[IpcClient] Sending command: '{command}'");
            var bytes = Encoding.UTF8.GetBytes(command + "\n");
            await client.WriteAsync(bytes, 0, bytes.Length, cts.Token).ConfigureAwait(false);
            await client.FlushAsync(cts.Token).ConfigureAwait(false);

            using var reader = new StreamReader(client, Encoding.UTF8);
            var response = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false);
            LogAction?.Invoke($"[IpcClient] Response received: '{response}'");
            return response;
        }
    }
}
