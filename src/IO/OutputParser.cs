namespace Ipk25Chat.IO
{ 
    public class OutputParser
    {
        public static Response Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new Response(ResponseType.Unknown, null, "Received empty or null message");

            string message = raw.TrimEnd('\r', '\n').Trim();

            try
            {
                if (message.StartsWith("REPLY"))
                {
                    return ParseReply(message);
                }
                else if (message.StartsWith("MSG FROM"))
                {
                    return ParseMessage(message);
                }
                else if (message.StartsWith("ERR FROM"))
                {
                    return ParseError(message);
                }
                else if (message.StartsWith("BYE FROM"))
                {
                    return ParseBye(message);
                }
                else
                {
                    return new Response(ResponseType.Unknown, null, $"Unrecognized message: {message}");
                }
            }
            catch (Exception ex)
            {
                return new Response(ResponseType.Unknown, null, $"Failed to parse message: {ex.Message}");
            }
        }

        private static Response ParseReply(string message)
        {
            string[] parts = message.Split(" IS ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new FormatException("REPLY message missing 'IS' separator");

            string[] header = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (header.Length != 2 || header[0] != "REPLY")
                throw new FormatException("Invalid REPLY header");

            bool isSuccess = header[1] == "OK";
            ResponseType type = isSuccess ? ResponseType.ReplyOk : ResponseType.ReplyNok;
            string content = parts[1];

            return new Response(type, null, content, isSuccess);
        }

        private static Response ParseMessage(string message)
        {
            string[] parts = message.Split(" IS ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new FormatException("MSG message missing 'IS' separator");

            string[] header = parts[0].Split(" FROM ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (header.Length != 2 || header[0] != "MSG")
                throw new FormatException("Invalid MSG header");

            string displayName = header[1];
            string content = parts[1];

            return new Response(ResponseType.Msg, displayName, content);
        }

        private static Response ParseError(string message)
        {
            string[] parts = message.Split(" IS ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new FormatException("ERR message missing 'IS' separator");

            string[] header = parts[0].Split(" FROM ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (header.Length != 2 || header[0] != "ERR")
                throw new FormatException("Invalid ERR header");

            string displayName = header[1];
            string content = parts[1];

            return new Response(ResponseType.Err, displayName, content);
        }

        private static Response ParseBye(string message)
        {
            string[] parts = message.Split(" FROM ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || parts[0] != "BYE")
                throw new FormatException("Invalid BYE message format");

            string displayName = parts[1];

            return new Response(ResponseType.Bye, displayName);
        }
    }
}