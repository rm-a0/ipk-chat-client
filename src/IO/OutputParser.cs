namespace Ipk25Chat.IO
{
    public class OutputParser
    {
        public static Response Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new Response(ResponseType.Unknown);

            if (raw == "BYE")
                return ParseBye(raw);

            if (raw.StartsWith("REPLY OK IS"))
                return ParseReplyOk(raw);

            if (raw.StartsWith("REPLY NOK IS"))
                return ParseReplyNok(raw);

            if (raw.StartsWith("MSG FROM"))
                return ParseMessage(raw);

            if (raw.StartsWith("ERR FROM"))
                return ParseError(raw);

            return new Response(ResponseType.Unknown, null, raw);
        }

        private static Response ParseBye(string raw)
        {
            return new Response(ResponseType.Bye);
        }

        private static Response ParseReplyOk(string raw)
        {
            return new Response(ResponseType.ReplyOk, null, raw.Substring(11), true);
        }

        private static Response ParseReplyNok(string raw)
        {
            return new Response(ResponseType.ReplyNok, null, raw.Substring(12), false);
        }

        private static Response ParseMessage(string raw)
        {
            var parts = raw.Split(" IS ");
            if (parts.Length == 2)
                return new Response(ResponseType.Msg, parts[0].Substring(9), parts[1]);
            return new Response(ResponseType.Unknown);
        }

        private static Response ParseError(string raw)
        {
            var parts = raw.Split(" IS ");
            if (parts.Length == 2)
                return new Response(ResponseType.Err, parts[0].Substring(9), parts[1]);
            return new Response(ResponseType.Unknown);
        }
    }
}    