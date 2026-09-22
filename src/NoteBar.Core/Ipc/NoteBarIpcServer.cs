using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoteBar.Core.Ipc
{
    public class NoteBarIpcServer : IDisposable
    {
        public static Action<string> LogAction { get; set; }

        private readonly Func<string, string> _commandHandler;
        private CancellationTokenSource _cts;

        public NoteBarIpcServer(Func<string, string> commandHandler)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public void Start()
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream server = null;
                try
                {
                    server = new NamedPipeServerStream(
                        NoteBarIpcConstants.PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    LogAction?.Invoke("[IpcServer] Waiting for client connection...");
                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                    LogAction?.Invoke("[IpcServer] Client connected!");

                    var connectedServer = server;
                    server = null; // Hand off ownership

                    _ = Task.Run(() => HandleClientAsync(connectedServer), token);
                }
                catch (OperationCanceledException)
                {
                    server?.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    LogAction?.Invoke($"[IpcServer] Listen error: {ex.Message}");
                    server?.Dispose();
                    try
                    {
                        await Task.Delay(100, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream server)
        {
            using (server)
            {
                try
                {
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var command = await reader.ReadLineAsync().ConfigureAwait(false);
                    LogAction?.Invoke($"[IpcServer] Command received: '{command}'");

                    if (!string.IsNullOrEmpty(command))
                    {
                        string response;
                        if (command == "PING")
                        {
                            response = "PONG";
                        }
                        else
                        {
                            try
                            {
                                response = _commandHandler(command);
                            }
                            catch (Exception ex)
                            {
                                response = "ERROR:" + ex.Message;
                            }
                        }

                        LogAction?.Invoke($"[IpcServer] Response sending: '{response}'");
                        var replyBytes = Encoding.UTF8.GetBytes((response ?? "OK") + "\n");
                        await server.WriteAsync(replyBytes, 0, replyBytes.Length).ConfigureAwait(false);
                        await server.FlushAsync().ConfigureAwait(false);
                        LogAction?.Invoke("[IpcServer] Response sent successfully");
                    }
                }
                catch (Exception ex)
                {
                    LogAction?.Invoke($"[IpcServer] Client handler exception: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
