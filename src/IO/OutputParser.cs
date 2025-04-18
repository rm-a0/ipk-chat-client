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
            if (data == null || data.Length < 1)
                return new Response(ResponseType.Unknown, null, "Received empty or malformed UDP message");

            byte type = data[0];

            try
            {
                switch (type)
                {
                    case 0x00: // CONFIRM
                        if (data.Length != 3)
                            return new Response(ResponseType.Unknown, null, $"Malformed CONFIRM message: expected 3 bytes, got {data.Length}");
                        ushort refMessageId = (ushort)((data[1] << 8) | data[2]);
                        return new Response(ResponseType.Confirm, null, null, false, null, refMessageId); // No MessageID

                    case 0x01: // REPLY
                        if (data.Length < 6)
                            return new Response(ResponseType.Unknown, null, $"REPLY message too short: {data.Length} bytes");
                        ushort messageId = (ushort)((data[1] << 8) | data[2]);
                        bool isSuccess = data[3] == 1;
                        ushort refMsgId = (ushort)((data[4] << 8) | data[5]);
                        string content = ExtractString(data, 6);
                        ResponseType replyType = isSuccess ? ResponseType.ReplyOk : ResponseType.ReplyNok;
                        return new Response(replyType, null, content, isSuccess, messageId, refMsgId);

                    case 0x04: // MSG
                        if (data.Length < 4)
                            return new Response(ResponseType.Unknown, null, $"MSG message too short: {data.Length} bytes");
                        messageId = (ushort)((data[1] << 8) | data[2]);
                        string[] fields = ExtractStrings(data, 3, 2);
                        if (fields.Length != 2)
                            return new Response(ResponseType.Unknown, null, $"MSG message missing required fields: found {fields.Length}");
                        return new Response(ResponseType.Msg, fields[0], fields[1], false, messageId);

                    case 0xFE: // ERR
                        if (data.Length < 4)
                            return new Response(ResponseType.Unknown, null, $"ERR message too short: {data.Length} bytes");
                        messageId = (ushort)((data[1] << 8) | data[2]);
                        fields = ExtractStrings(data, 3, 2);
                        if (fields.Length != 2)
                            return new Response(ResponseType.Unknown, null, $"ERR message missing required fields: found {fields.Length}");
                        return new Response(ResponseType.Err, fields[0], fields[1], false, messageId);

                    case 0xFF: // BYE
                        if (data.Length < 4)
                            return new Response(ResponseType.Unknown, null, $"BYE message too short: {data.Length} bytes");
                        messageId = (ushort)((data[1] << 8) | data[2]);
                        string displayName = ExtractString(data, 3);
                        if (string.IsNullOrEmpty(displayName))
                            return new Response(ResponseType.Unknown, null, "BYE message missing DisplayName");
                        return new Response(ResponseType.Bye, displayName, null, false, messageId);

                    case 0xFD: // PING
                        if (data.Length < 3)
                            return new Response(ResponseType.Unknown, null, $"PING message too short: {data.Length} bytes");
                        messageId = (ushort)((data[1] << 8) | data[2]);
                        return new Response(ResponseType.Ping, null, "PING message received", false, messageId);

                    default:
                        return new Response(ResponseType.Unknown, null, $"Unrecognized UDP message type: 0x{type:X2}");
                }
            }
            catch (Exception ex)
            {
                return new Response(ResponseType.Unknown, null, $"Failed to parse UDP message: {ex.Message}");
            }
        }

        private static string ExtractString(byte[] data, int startIndex)
        {
            int endIndex = startIndex;
            while (endIndex < data.Length && data[endIndex] != 0)
                endIndex++;
            if (endIndex >= data.Length || endIndex == startIndex)
                throw new FormatException("Invalid string termination in UDP message");
            return Encoding.ASCII.GetString(data, startIndex, endIndex - startIndex);
        }

        private static string[] ExtractStrings(byte[] data, int startIndex, int count)
        {
            List<string> result = new List<string>();
            int index = startIndex;

            for (int i = 0; i < count && index < data.Length; i++)
            {
                string str = ExtractString(data, index);
                result.Add(str);
                index += str.Length + 1;
            }

            if (result.Count != count)
                throw new FormatException($"Expected {count} strings, found {result.Count}");
            return result.ToArray();
        }
    }
}