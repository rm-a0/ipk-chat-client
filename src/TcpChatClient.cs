using System.Net.Sockets;

public class TcpChatClient : ChatClient
{
    private readonly string _server;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _isConnected;
    private string _displayName = "User";

    public TcpChatClient(string server, int port)
    {
        _server = server;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        _client = new TcpClient();
        await _client.ConnectAsync(_server, _port);
        _isConnected = true;
        Debugger.Log("Connected to TCP server");
    }
}