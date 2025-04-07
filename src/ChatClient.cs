public interface ChatClient
{   
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendMessageAsync(Command command);
    Task ListenToServerAsync(ChatStateMachine stateMachine, CancellationToken token);
}