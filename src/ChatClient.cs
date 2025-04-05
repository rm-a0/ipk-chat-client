public interface ChatClient
{   
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendMessageAsync();
}