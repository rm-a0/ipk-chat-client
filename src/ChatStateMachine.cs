using System;
using System.Data;
using System.Reflection.Metadata.Ecma335;

public enum ClientState
{
    Start,
    Auth,
    Open,
    Join,
    End
}

public class ChatStateMachine
{
    private ClientState _state = ClientState.Start;
    private readonly ChatClient _client;

    public ChatStateMachine(ChatClient client)
    {
        _client = client;
    }

    public async Task HandleCommandAsync(Command command)
    {
        switch (_state)
        {
            case ClientState.Start:
                if (command.Type == CommandType.Bye)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.End;
                }
                else if (command.Type == CommandType.Auth)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.Auth;
                }
                else
                {
                    Console.WriteLine($"ERROR: Must authenticate before sending {command.Type}");
                    // add cancelation method
                }
                break;
            case ClientState.Auth:
                if (command.Type == CommandType.Bye)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.End;
                }
                else if (command.Type == CommandType.Auth)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.Auth;
                }
                // TODO: ERR
                else
                {
                    Console.WriteLine("ERROR: Waiting for response from the server");
                }
                break;
            case ClientState.Open:
                if (command.Type == CommandType.Bye)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.End;
                }
                else if (command.Type == CommandType.Msg)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.Open;
                }
                else if (command.Type == CommandType.Join)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.Join;
                }
                // TODO: ERR
                else
                {
                    Console.WriteLine($"ERROR: State Open: Invalid command {command.Type}");
                }
                break;
            case ClientState.Join:
                if (command.Type == CommandType.Bye)
                {
                    await _client.SendMessageAsync(command);
                    _state = ClientState.End;
                }
                break;
            case ClientState.End:
                Debugger.Log("Reached end state");
                break;
        }
    }

    public void HandleResponse(string response)
    {
        switch (_state)
        {
            case ClientState.Start:
                break;

        }
    }
}