public enum ResponseType
{
    Unknown,
    ReplyOk,
    ReplyNok,
    Msg,
    Err,
    Bye
}

public class Response
{
    public ResponseType Type { get; }
    public string? Sender { get; }
    public string? Content { get; }
    public bool IsSuccess { get; }

    public Response(ResponseType type, string? sender = null, string? content = null, bool isSuccess = false)
    {
        Type = type;
        Sender = sender;
        Content = content;
        IsSuccess = isSuccess;
    }

    public static Response Parse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return new Response(ResponseType.Unknown);

        if (raw == "BYE")
            return new Response(ResponseType.Bye);

        if (raw.StartsWith("REPLY OK IS"))
            return new Response(ResponseType.ReplyOk, null, raw.Substring(11), true);

        if (raw.StartsWith("REPLY NOK IS"))
            return new Response(ResponseType.ReplyNok, null, raw.Substring(12), false);

        if (raw.StartsWith("MSG FROM"))
        {
            var parts = raw.Split(" IS ");
            if (parts.Length == 2)
                return new Response(ResponseType.Msg, parts[0].Substring(9), parts[1]);
        }

        if (raw.StartsWith("ERR FROM"))
        {
            var parts = raw.Split(" IS ");
            if (parts.Length == 2)
                return new Response(ResponseType.Err, parts[0].Substring(9), parts[1]);
        }

        return new Response(ResponseType.Unknown, null, raw);
    }
}