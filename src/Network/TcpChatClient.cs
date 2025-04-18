using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Ipk25Chat.Network
{
    public class TcpChatClient : ChatClient
    {
        private readonly string _server;
        private readonly int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isConnected;
        private int _timeout;
        private bool _shouldExit = false;
        private CancellationTokenSource? _replyTimeoutCts;

        public TcpChatClient(string server, int port)
        {
            _server = server;
            _port = port;
            _timeout = 5000;
        }

        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_server, _port);
            _stream = _client.GetStream();
            _isConnected = true;
            _replyTimeoutCts = new CancellationTokenSource();
            Debugger.Log("Connected to TCP server");
        }

        public async Task ListenToServerAsync(ChatStateMachine stateMachine, CancellationToken token)
        {
        byte[] buffer = new byte[1024];

            while (_isConnected && !token.IsCancellationRequested && (_stream !=null))
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead == 0)
                    {
                        _isConnected = false;
                        Debugger.Log("Server disconnected.");
                        break;
                    }

                    string output = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Debugger.Log($"Raw ouput received: {output}");
                    Response response = OutputParser.Parse(output);

                    if (response.Type == ResponseType.ReplyNok || response.Type == ResponseType.ReplyOk) {
                        if (_replyTimeoutCts != null)
                        {
                            _replyTimeoutCts.Cancel();
                            _replyTimeoutCts.Dispose();
                            _replyTimeoutCts = null;
                            Debugger.Log("Reply timeout timer canceled due to REPLY received");
                        }
                    }
                    _ = stateMachine.HandleResponse(response);
                }
                catch (OperationCanceledException)
                {
                    Debugger.Log("Listening to server has been canceled.");
                    break; // Exit the loop when canceled
                }
                catch (Exception ex)
                {
                    // Handle any other exception (network errors, etc.)
                    Console.WriteLine($"ERROR: {ex.Message}");
                    break;
                }
            } 
        }

        public async Task DisconnectAsync()
        {
            if (_client != null && _client.Connected)
            {
                _isConnected = false;

                if (_stream != null)
                {
                    await _stream.FlushAsync();
                    _stream.Close();
                    Debugger.Log("NetworkStream closed.");
                }

                _client.Close();
                Debugger.Log("TcpClient closed.");
            }
        }

        public async Task SendMessageAsync(Command command)
        {
            if (!_isConnected || _stream == null)
            {
                throw new InvalidOperationException("Not connected to the server");
            }

            if (command.IsLocal || command.Type == CommandType.Unknown)
            {
                return;
            }

            if (command.Type == CommandType.Auth || command.Type == CommandType.Join) {
                if (_replyTimeoutCts != null)
                {
                    _replyTimeoutCts.Dispose();
                }
                _replyTimeoutCts = new CancellationTokenSource();
                _ = StartReplyTimeoutAsync(command.Type);
            }

            string formattedMessage = command.ToTcpString();
            Debugger.Log($"Sending: {formattedMessage}");
            byte[] data = Encoding.ASCII.GetBytes(formattedMessage + "\r\n");
            await _stream.WriteAsync(data, 0, data.Length);
            
        }

        private async Task StartReplyTimeoutAsync(CommandType commandType)
        {
            try
            {
                Debugger.Log($"Starting {_timeout/1000} second timeout for {commandType}");
                await Task.Delay(_timeout, _replyTimeoutCts!.Token);
                Console.WriteLine($"ERROR: No REPLY received for {commandType} within {_timeout} ms");
                _shouldExit = true;
            }
            catch (TaskCanceledException) { }
        }

        public bool ShouldExit() => _shouldExit;
    }
}
