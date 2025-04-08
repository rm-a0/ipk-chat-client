using System.Net.Sockets;
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

        public TcpChatClient(string server, int port)
        {
            _server = server;
            _port = port;
        }

        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_server, _port);
            _stream = _client.GetStream();
            _isConnected = true;
            Debugger.Log("Connected to TCP server");
        }

        public async Task ListenToServerAsync(ChatStateMachine stateMachine, CancellationToken token)
        {
        byte[] buffer = new byte[1024];

            while (_isConnected && !token.IsCancellationRequested)
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

                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Debugger.Log($"Response received: {response}");
                    stateMachine.HandleResponse(response);
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

            string formattedMessage = FormatCommand(command);
            Debugger.Log($"Sending: {formattedMessage}");
            byte[] data = Encoding.ASCII.GetBytes(formattedMessage + "\r\n");
            await _stream.WriteAsync(data, 0, data.Length);
            
        }

        private string FormatCommand(Command command)
        {
            return command.Type switch
            {
                CommandType.Auth => $"AUTH {command.Username} AS {command.DisplayName} USING {command.Secret}",
                CommandType.Join => $"JOIN {command.Channel} AS {command.DisplayName}",
                CommandType.Msg => $"MSG FROM {command.DisplayName} IS {command.Content}",
                CommandType.Bye => "BYE",
                CommandType.Rename => throw new InvalidOperationException("Rename is a local command and should not be formatted."),
                CommandType.Help => throw new InvalidOperationException("Help is a local command and should not be formatted."),
                CommandType.Unknown => throw new InvalidOperationException("Unknown command should not be formatted."),
                _ => throw new InvalidOperationException("Unexpected command type.") // Catch-all for future enum values
            };
        }
    }
}
