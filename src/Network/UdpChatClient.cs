using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ipk25Chat.Network
{
    public class UdpChatClient : ChatClient
    {
        private readonly string _server;
        private readonly int _port;
        private readonly int _timeout;
        private readonly int _retries;
        private UdpClient? _client;
        private IPEndPoint? _serverEndPoint;
        private bool _isConnected;
        private ushort _nextMessageId;
        private bool _shouldExit = false;
        private CancellationTokenSource? _confirmTimeoutCts;
        private readonly object _pendingLock = new object();
        private ushort? _pendingMessageId;

        public UdpChatClient(string server, int port, int timeout, int retries)
        {
            _server = server;
            _port = port;
            _timeout = timeout;
            _retries = retries;
            _nextMessageId = 0;
        }

        public async Task ConnectAsync()
        {
            _client = new UdpClient(0);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(_server), _port);
            _isConnected = true;
            _confirmTimeoutCts = new CancellationTokenSource();
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

                    if (response.Type == ResponseType.Confirm) {
                        if (response.RefMessageId.HasValue)
                        {
                        }
                        continue; // Confirm wont be passed to state machine
                    }

                    _serverEndPoint = remoteEndPoint;
                    _ = stateMachine.HandleResponse(response);
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

            byte[] data = command.ToUdpBytes(_nextMessageId);
            ushort messageId = _nextMessageId;
            _nextMessageId++;

            Debugger.Log($"Sending: {BitConverter.ToString(data)} (MessageID: {messageId})");

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

        private async Task StartConfirmTimeoutAsync(CommandType commandType, byte[] data, ushort messageId)
        {
            int attempts = 0;
            bool confirmed = false;

            while (attempts <= _retries && !confirmed && _isConnected)
            {
                try
                {
                    if (_client == null || _serverEndPoint == null)
                    {
                        throw new InvalidOperationException("Client not initialized");
                    }

                    await _client.SendAsync(data, data.Length, _serverEndPoint);
                    Debugger.Log($"Sent command {commandType}, attempt {attempts + 1}/{_retries + 1}, MessageID: {messageId}");

                    try
                    {
                        await Task.Delay(_timeout, _confirmTimeoutCts!.Token);
                        attempts++;
                        Debugger.Log($"Timeout waiting for CONFIRM for MessageID {messageId}, attempt {attempts}/{_retries}");
                    }
                    catch (TaskCanceledException)
                    {
                        confirmed = true; // CONFIRM received, timer canceled
                    }
                }
                catch (SocketException ex)
                {
                    Debugger.Log($"Failed to send message: {ex.Message}");
                    lock (_pendingLock)
                    {
                        _pendingMessageId = null;
                    }
                    Console.WriteLine($"ERROR: Network error sending {commandType}: {ex.Message}");
                    _isConnected = false;
                    _shouldExit = true;
                    await DisconnectAsync();
                    return;
                }
            }

            lock (_pendingLock)
            {
                if (_pendingMessageId == messageId)
                {
                    _pendingMessageId = null;
                }
            }

            if (!confirmed)
            {
                Console.WriteLine($"ERROR: No CONFIRM received for {commandType} after {_retries + 1} attempts");
                _isConnected = false;
                _shouldExit = true;
                await DisconnectAsync();
            }
        }

        public bool ShouldExit() => _shouldExit;
    }
}