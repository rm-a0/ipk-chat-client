using System.Text.RegularExpressions;

namespace Ipk25Chat.IO
{
    public enum CommandType
    {
        Auth,
        Join,
        Rename,
        Msg,
        Bye,
        Err,
        Help,
        Unknown
    }

    public class Command
    {
        private static readonly Regex NamePattern = new Regex(@"^[a-zA-Z0-9_-]*$", RegexOptions.Compiled);
        private static readonly Regex PrintablePattern = new Regex(@"^[\x21-\x7E]*$", RegexOptions.Compiled);
        private static readonly Regex ContentPattern = new Regex(@"^[\x0A\x20-\x7E]*$", RegexOptions.Compiled);

        public CommandType Type { get; }
        public string? Username { get; }
        public string? Secret { get; }
        public string? DisplayName { get; }
        public string? Channel { get; }
        public string? Content { get; }
        public bool IsLocal { get; }

        public Command(CommandType type, string? username = null, string? secret = null, 
                    string? displayName = null, string? channel = null, string? content = null, 
                    bool isLocal = false)
        {
            Type = type;
            IsLocal = isLocal;

            if (username != null)
            {
                if (username.Length > 20) throw new ArgumentException("Username must not exceed 20 characters.");
                if (!NamePattern.IsMatch(username)) throw new ArgumentException("Username must contain only [a-zA-Z0-9_-].");
                Username = username;
            }

            if (secret != null)
            {
                if (secret.Length > 128) throw new ArgumentException("Secret must not exceed 128 characters.");
                if (!NamePattern.IsMatch(secret)) throw new ArgumentException("Secret must contain only [a-zA-Z0-9_-].");
                Secret = secret;
            }

            if (displayName != null)
            {
                if (displayName.Length > 20) throw new ArgumentException("DisplayName must not exceed 20 characters.");
                if (!PrintablePattern.IsMatch(displayName)) throw new ArgumentException("DisplayName must contain only printable characters (0x21-7E).");
                DisplayName = displayName;
            }

            if (channel != null)
            {
                if (channel.Length > 20) throw new ArgumentException("ChannelID must not exceed 20 characters.");
                if (!NamePattern.IsMatch(channel)) throw new ArgumentException("ChannelID must contain only [a-zA-Z0-9_-].");
                Channel = channel;
            }

            if (content != null)
            {
                if (content.Length > 60000) throw new ArgumentException("MessageContent must not exceed 60,000 characters.");
                if (!ContentPattern.IsMatch(content)) throw new ArgumentException("MessageContent must contain only printable characters, space, or line feed (0x0A, 0x20-7E).");
                Content = content;
            }
        }

        public string ToTcpString()
        {
            if (IsLocal || Type == CommandType.Unknown)
                throw new InvalidOperationException($"{Type} is a local or unknown command and cannot be formatted for TCP.");

            return Type switch
            {
                CommandType.Auth => $"AUTH {Username} AS {DisplayName} USING {Secret}",
                CommandType.Join => $"JOIN {Channel} AS {DisplayName}",
                CommandType.Msg => $"MSG FROM {DisplayName} IS {Content}",
                CommandType.Err => $"ERR FROM {DisplayName} IS {Content}",
                CommandType.Bye => $"BYE FROM {DisplayName}",
                _ => throw new InvalidOperationException($"Unexpected command type: {Type}")
            };
        }
    }
}