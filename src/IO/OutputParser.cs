using System.Text;

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

        public static Response Parse(byte[] data)
        {
            if (data.Length < 3)
                throw new ArgumentException("UDP message too short");

            byte type = data[0];
            ushort messageId = (ushort)(data[1] << 8 | data[2]);

            switch (type)
            {
                case 0x00: // CONFIRM
                    if (data.Length != 3)
                        throw new ArgumentException("Invalid CONFIRM format: must be 3 bytes");
                    return new Response(ResponseType.Confirm, null, null, false, messageId, messageId);

                case 0x01: // REPLY
                    if (data.Length < 6)
                        throw new ArgumentException("Invalid REPLY format: too short");
                    bool isSuccess = data[3] == 0x01;
                    ushort refMessageId = (ushort)(data[4] << 8 | data[5]);
                    string content = Encoding.ASCII.GetString(data, 6, data.Length - 6).TrimEnd('\0');
                    ResponseType replyType = isSuccess ? ResponseType.ReplyOk : ResponseType.ReplyNok;
                    return new Response(replyType, null, content, isSuccess, messageId, refMessageId, true);

                case 0x04: // MSG
                case 0xFE: // ERR
                    if (data.Length < 4)
                        throw new ArgumentException("Invalid MSG/ERR format: too short");
                    var parts = Encoding.ASCII.GetString(data, 3, data.Length - 3)
                        .Split('\0', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        throw new ArgumentException("Invalid MSG/ERR format: expected DisplayName and Content");
                    ResponseType msgType = type == 0x04 ? ResponseType.Msg : ResponseType.Err;
                    return new Response(msgType, parts[0], parts[1], false, messageId, null, true);

                case 0xFF: // BYE
                    if (data.Length < 4)
                        throw new ArgumentException("Invalid BYE format: too short");
                    string displayName = Encoding.ASCII.GetString(data, 3, data.Length - 4).TrimEnd('\0');
                    return new Response(ResponseType.Bye, displayName, null, false, messageId, null, true);

                case 0xFD: // PING
                    return new Response(ResponseType.Ping, null, null, false, messageId, null, true);

                default:
                    return new Response(ResponseType.Unknown, null, $"Unknown UDP message type: 0x{type:X2}", false, messageId);
            }
        }
    }
}