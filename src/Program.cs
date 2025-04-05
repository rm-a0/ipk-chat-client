using System.Net.Sockets;

class Program
{
    static async Task Main(string[] args)
    {
        var parser = new ArgumentParser();
        ChatClient? client = null;
        try
        {
            parser.Parse(args);

            Debugger.Enable(parser.Debug);
            Debugger.Log("Debugger enabled");

            client = parser.Protocol switch
            {
                "tcp" => new TcpChatClient(parser.Server, parser.Port),
                "udp" => new UdpChatClient(parser.Server, parser.Port),
                _ => throw new InvalidOperationException("Unsupported protocol")
            };
            await client.ConnectAsync();

            string? input;
            while ((input = Console.ReadLine()) != null)
            {
                Debugger.Log($"User input from console: {input}");
                await client.SendMessageAsync(input);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
    }
}