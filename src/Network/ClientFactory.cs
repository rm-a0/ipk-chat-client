namespace Ipk25Chat.Network
{
    public static class ChatClientFactory
    {
        public static ChatClient Create(string protocol, string server, int port, int timeout, int retries)
        {
            return protocol switch
            {
                "tcp" => new TcpChatClient(server, port),
                "udp" => new UdpChatClient(server, port, timeout, retries),
                _ => throw new InvalidOperationException("Unsupported protocol")
            };
        }
    }
}
    