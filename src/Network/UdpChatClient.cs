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
        private bool _shouldExit = false;
        private CancellationTokenSource? _confirmTimeoutCts;
        private readonly object _pendingLock = new object();
        private ushort _nextMessageId;
        private ushort? _pendingMessageId;

        public UdpChatClient(string server, int port, int timeout, int retries)
        {
            _server = server;
            _port = port;
            _timeout = timeout;
            _retries = 0;
            _nextMessageId = 0;
        }

        public async Task ConnectAsync()
        {
            _client = new UdpClient(0);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(_server), _port);
            _isConnected = true;
            _confirmTimeoutCts = new CancellationTokenSource();
            Debugger.Log("Connected to UDP endpoint");
            await Task.CompletedTask;
        }

        public async Task ListenToServerAsync(ChatStateMachine stateMachine, CancellationToken token)
        {
            if (_client == null || _serverEndPoint == null)
            {
                throw new InvalidOperationException("Client not initialized");
            }

            while (_isConnected && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await _client.ReceiveAsync(token);
                    byte[] receivedData = result.Buffer;
                    IPEndPoint remoteEndPoint = result.RemoteEndPoint;

                    Debugger.Log($"Raw output received: {BitConverter.ToString(receivedData)} from {result.RemoteEndPoint}");
                    Response response = OutputParser.Parse(receivedData);

                    if (response.Type == ResponseType.Confirm)
                    {

                        if (response.RefMessageId.HasValue)
                        {
                            HandleConfirm(response.RefMessageId.Value);
                        }
                        continue;
                    }

                    _serverEndPoint = remoteEndPoint;

                    if (response.MessageId.HasValue && response.ShouldConfirm)
                    {
                        await SendConfirmAsync(response.MessageId.Value);
                    }

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
            await Task.CompletedTask;
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

            lock (_pendingLock)
            {
                if (_pendingMessageId.HasValue)
                {
                    throw new InvalidOperationException("Another message is awaiting confirmation");
                }
            }

            byte[] data = command.ToUdpBytes(_nextMessageId);
            ushort messageId = _nextMessageId;
            _nextMessageId++;

            lock (_pendingLock)
            {
                _pendingMessageId = messageId;
            }

            _confirmTimeoutCts?.Dispose();
            _confirmTimeoutCts = new CancellationTokenSource();
            _ = SendWithTimeoutAsync(command.Type, data, messageId);
            await Task.CompletedTask;
        }

        private async Task SendWithTimeoutAsync(CommandType commandType, byte[] data, ushort messageId)
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
                        confirmed = true;
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
                _shouldExit = true;
            }
        }

        private void HandleConfirm(ushort refMessageId)
        {
            lock (_pendingLock)
            {
                if (_pendingMessageId == refMessageId)
                {
                    _pendingMessageId = null;
                    _confirmTimeoutCts?.Cancel();
                    Debugger.Log($"Received CONFIRM for MessageID {refMessageId}");
                }
            }
        }

        private async Task SendConfirmAsync(ushort refMessageId)
        {
            if (_client == null) return;

            byte[] confirmData = new byte[3];
            confirmData[0] = 0x00;
            confirmData[1] = (byte)(refMessageId >> 8);
            confirmData[2] = (byte)(refMessageId & 0xFF);

            try
            {
                Debugger.Log($"Preparing to send CONFIRM for REPLY MessageID {refMessageId}");
                await _client.SendAsync(confirmData, confirmData.Length, _serverEndPoint);
                Debugger.Log($"Sent CONFIRM for MessageID {refMessageId} to {_serverEndPoint}");
                Debugger.Log($"Sending CONFIRM: {BitConverter.ToString(confirmData)}");
            }
            catch (SocketException ex)
            {
                Debugger.Log($"Failed to send CONFIRM: {ex.Message}");
            }
        }

        public bool ShouldExit() => _shouldExit;
    }
}