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
        public string? DisplayName { get; }
        public string? Content { get; }
        public bool IsSuccess { get; }

        public Response(ResponseType type, string? displayName = null, string? content = null, bool isSuccess = false)
        {
            Type = type;
            DisplayName = displayName;
            Content = content;
            IsSuccess = isSuccess;
        }

        public override string ToString()
        {
            return Type switch
            {
                ResponseType.ReplyOk => $"Action Success: {Content}",
                ResponseType.ReplyNok => $"Action Failure: {Content}",
                ResponseType.Msg => $"{DisplayName}: {Content}",
                ResponseType.Err => $"ERROR FROM {DisplayName}: {Content}",
                ResponseType.Bye => $"BYE FROM {DisplayName}",
                ResponseType.Unknown => $"ERROR: Unknown response: {Content}",
                _ => "Invalid response"
            };
        }
    }
}