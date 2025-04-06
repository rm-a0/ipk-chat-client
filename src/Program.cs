class Program
{
    static async Task Main(string[] args)
    {
        var parser = new ArgumentParser();
        var inputParser = new InputParser();
        ChatClient? client = null;

        try
        {
            parser.Parse(args);

            Debugger.Enable(parser.Debug);
            Debugger.Log("Debugger enabled");
            Debugger.Log($"Server IP: {parser.Server}");

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
                try
                {
                    Command command = inputParser.Parse(input);
                    if (!command.IsLocal && command.Type != CommandType.Unknown)
                    {
                        await client.SendMessageAsync(command);
                    }
                    else if (command.Type == CommandType.Help)
                    {
                        Console.WriteLine("Commands:");
                        Console.WriteLine("  /auth <username> <secret> <displayname>  Send 'AUTH' message to server, locally set 'displayName'");
                        Console.WriteLine("  /join <channel>                          Send 'JOIN' message to server");
                        Console.WriteLine("  /rename <displayName>                    Locally change display name");
                        Console.WriteLine("  /bye                                     Disconnect from the server");
                        Console.WriteLine("  <message>                                Send 'MSG' message to the server with 'message' as content");
                        Console.WriteLine("  /help                                    Display this message");
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
    }
}