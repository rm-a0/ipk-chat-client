using System.Diagnostics.Contracts;

public class InputParser
{
    private string _displayName;

    public InputParser(string initialDisplayName = "User")
    {
        _displayName = initialDisplayName;
    }

    public Command Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new Command(CommandType.Unknown);
        }

        if (input.StartsWith("/"))
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0].ToLower())
            {
                case "/auth":
                    if (parts.Length != 4)
                    {
                        throw new ArgumentException("Invalid syntax. Usage: /auth <username> <secret> <displayname>");
                    }
                    _displayName = parts[3];
                    return new Command(CommandType.Auth, username: parts[1], secret: parts[2], displayName: parts[3]);

                case "/join":
                    if (parts.Length != 2)
                    {
                        throw new ArgumentException("Invalid syntax. Usage: /join <channel>");
                    }
                    return new Command(CommandType.Join, channel: parts[1], displayName: _displayName);

                case "/rename":
                    if (parts.Length != 2)
                    {
                        throw new ArgumentException("Invalid syntax. Usage: /rename <displayname>");
                    }
                    _displayName = parts[1];
                    return new Command(CommandType.Rename, displayName: parts[1], isLocal: true);

                case "/help":
                    PrintHelp();
                    return new Command(CommandType.Help);

                case "/bye":
                    return new Command(CommandType.Bye);

                default:
                    throw new ArgumentException("Unknown command");
            }
        }

        return new Command(CommandType.Msg, displayName: _displayName, content: input);
    }

    private void PrintHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  /auth <username> <secret> <displayname>  Send 'AUTH' message to server, locally set 'displayName'");
        Console.WriteLine("  /join <channel>                          Send 'JOIN' message to server");
        Console.WriteLine("  /rename <displayName>                    Locally change display name");
        Console.WriteLine("  /bye                                     Disconnect from the server");
        Console.WriteLine("  <message>                                Send 'MSG' message to the server with 'message' as content");
        Console.WriteLine("  /help                                    Display this message");
    }

    public string GetDisplayName() => _displayName;
}