namespace Ipk25Chat.IO
{
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
    }
}