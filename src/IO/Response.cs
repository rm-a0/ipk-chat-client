namespace Ipk25Chat.IO
{
    public enum ResponseType
    {
        Unknown,
        ReplyOk,
        ReplyNok,
        Msg,
        Err,
        Confirm,
        Ping,
        Bye
    }

    public class Response
    {
        public ResponseType Type { get; }
        public string? DisplayName { get; }
        public string? Content { get; }
        public bool IsSuccess { get; }
        public ushort? MessageId { get; }
        public ushort? RefMessageId { get; }
        public bool ShouldConfirm { get; }

        public Response(ResponseType type, string? displayName = null, 
                        string? content = null, bool isSuccess = false, 
                        ushort? messageId = null, ushort? refMessageId = null,
                        bool shouldConfirm = false)
        {
            Type = type;
            DisplayName = displayName;
            Content = content;
            IsSuccess = isSuccess;
            MessageId = messageId;
            RefMessageId = refMessageId;
            ShouldConfirm= shouldConfirm;
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
                ResponseType.Confirm => $"Confirm for MessageID {MessageId}",
                ResponseType.Ping=> $"PING received",
                ResponseType.Unknown => $"ERROR: Unknown response: {Content}",
                _ => "ERROR: Invalid response"
            };
        }
    }
}