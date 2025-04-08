using System.Net.Sockets;

namespace Ipk25Chat.Network
{
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

        public async Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SendMessageAsync(Command command)
        {
            throw new NotImplementedException();
        }

        public async Task ListenToServerAsync(ChatStateMachine stateMachine, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
