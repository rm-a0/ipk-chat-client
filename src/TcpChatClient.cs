using System.Net.Sockets;
using System.Text;
using System.Runtime.CompilerServices;

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
        _ = Task.Run(ReceiveMessageAsync);
    }

    private async Task ReceiveMessageAsync()
    {
        if (_stream == null)
        {
            Debugger.Log("Stream is null");
            return;
        }

        byte[] buffer = new byte[1024];
        while (_isConnected)
        {
            try
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    _isConnected = false;
                    Debugger.Log("Server disconnected (bytesRead = 0)");
                    throw new IOException("Server closed the connection");
                }

                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');
                Debugger.Log($"Received: {response}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to receive message from server");
            }
        }
    }

    public async Task DisconnectAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SendMessageAsync(string message)
    {
        if (!_isConnected || _stream == null)
        {
            throw new InvalidOperationException("Not connected to the server");
        }
        
        string? formattedMessage = "";
        if (string.IsNullOrEmpty(formattedMessage))
        {
            Debugger.Log("String is null or empty");
            return;
        }

        Debugger.Log($"Sending: '{formattedMessage}'");
        byte[] data = Encoding.ASCII.GetBytes(formattedMessage + "\r\n");
        await _stream.WriteAsync(data, 0, data.Length);
        
    }
}