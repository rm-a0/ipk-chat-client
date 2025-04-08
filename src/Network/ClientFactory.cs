namespace Ipk25Chat.Network
{
    public static class ChatClientFactory
    {
        public static ChatClient Create(string protocol, string server, int port)
        {
            return protocol switch
            {
                "tcp" => new TcpChatClient(server, port),
                "udp" => new UdpChatClient(server, port),
                _ => throw new InvalidOperationException("Unsupported protocol")
            };
        }
    }
}
    