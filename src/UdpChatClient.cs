using System.Net.Sockets;
using System.Runtime.CompilerServices;

public class UdpChatClient : ChatClient
{
    private readonly string _server;
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _isConnected;

    public UdpChatClient(string server, int port)
    {
        _server = server;
        _port = port;
    }

    public async Task ConnectAsync()
    {
        throw new NotImplementedException();
    }

    public Task DisconnectAsync()
    {
        throw new NotImplementedException();
    }

    public Task SendMessageAsync()
    {
        throw new NotImplementedException();
    }
}