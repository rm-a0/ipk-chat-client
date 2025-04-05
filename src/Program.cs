using System.Net.Sockets;

class Program
{
    static async Task Main(string[] args)
    {
        var parser = new ArgumentParser();
        try
        {
            parser.Parse(args);

            Debugger.Enable(parser.Debug);
            Debugger.Log("Debugger enabled");

            ChatClient? client = parser.Protocol switch
            {
                "tcp" => new TcpChatClient(parser.Server, parser.Port),
                "udp" => new UdpChatClient(parser.Server, parser.Port),
                _ => throw new InvalidOperationException("Unsupported protocol")
            };
            await client.ConnectAsync();
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Argument error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}