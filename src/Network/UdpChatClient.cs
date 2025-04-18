using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ipk25Chat.Network
{
    public class UdpChatClient : ChatClient
    {
        private readonly string _server;
        private readonly int _port;
        private UdpClient? _client;
        private IPEndPoint? _serverEndPoint;
        private bool _isConnected;
        private readonly int _timeout;
        private readonly int _retries;

        public UdpChatClient(string server, int port, int timeout, int retries)
        {
            _server = server;
            _port = port;
            _timeout = timeout;
            _retries = retries;
        }

        public async Task ConnectAsync()
        {
            _client = new UdpClient(0);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(_server), _port);
            _isConnected = true;
            Debugger.Log("Connected to UDP endpoint");
        }

        public async Task ListenToServerAsync(ChatStateMachine stateMachine, CancellationToken token)
        {
            if (_client == null || _serverEndPoint == null)
            {
                throw new InvalidOperationException("Client not initialized");
            }

            byte[] buffer = new byte[65535];

            while (_isConnected && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await _client.ReceiveAsync(token);
                    byte[] receivedData = result.Buffer;
                    IPEndPoint remoteEndPoint = result.RemoteEndPoint;

                    Debugger.Log($"Raw output received: {BitConverter.ToString(receivedData)} from {result.RemoteEndPoint}");
                    Response response = OutputParser.Parse(receivedData);

                    // handle confirm later

                    _serverEndPoint = remoteEndPoint;
                    stateMachine.HandleResponse(response);
                }
                catch (OperationCanceledException)
                {
                    Debugger.Log("Listening to server has been canceled.");
                    break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"ERROR: Network error - {ex.Message}");
                    _isConnected = false;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    _isConnected = false;
                    break;
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (_client != null)
            {
                _isConnected = false;
                _client.Close();
                _client.Dispose();
                _client = null;
                Debugger.Log("UDP client closed.");
            }
        }

        public async Task SendMessageAsync(Command command)
        {
            if (!_isConnected || _client == null || _serverEndPoint == null)
            {
                throw new InvalidOperationException("Not connected to the server");
            }

            if (command.IsLocal || command.Type == CommandType.Unknown)
            {
                return;
            }

            byte[] data = command.ToUdpBytes();
            Debugger.Log($"Sending: {BitConverter.ToString(data)}");

            try
            {
                await _client.SendAsync(data, data.Length, _serverEndPoint);
            }
            catch (SocketException ex)
            {
                Debugger.Log($"Failed to send message: {ex.Message}");
                throw;
            }
        }

        public int GetTimeout() => _timeout;
        public int GetRetries() => _retries;
    }
}