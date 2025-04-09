using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;

namespace Ipk25Chat.Core
{
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
        public event EventHandler? RequestExit;

        private ClientState _state = ClientState.Start;
        private readonly ChatClient _client;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ChatStateMachine(ChatClient client)
        {
            _client = client;
            _ = Task.Run(() => EndStateListenerAsync());
        }

        private async Task EndStateListenerAsync()
        {
            while (true)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_state == ClientState.End)
                    {
                        RequestExit?.Invoke(this, EventArgs.Empty);
                        return; // Exit the loop once End is reached
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                await Task.Delay(50); // Small delay to prevent busy-waiting
            }
        }

        public async Task HandleCommandAsync(Command command)
        {
            await _semaphore.WaitAsync();
            try
            {
                bool shouldSendMessage = false;
                switch (_state)
                {
                    case ClientState.Start:
                        shouldSendMessage = HandleSendStartState(command);
                        break;
                    case ClientState.Auth:
                        shouldSendMessage = HandleSendAuthState(command);
                        break;
                    case ClientState.Open:
                        shouldSendMessage = HandleSendOpenState(command);
                        break;
                    case ClientState.Join:
                        shouldSendMessage = HandleSendJoinState(command);
                        break;
                    case ClientState.End:
                        Debugger.Log("Reached end state, no commands will be sent");
                        return;
                }

                if (shouldSendMessage && command.Type != CommandType.Unknown)
                {
                    await _client.SendMessageAsync(command);
                    Debugger.Log($"HandleCommand: Sent command '{command.Type}'");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task HandleResponse(Response response)
        {
            _semaphore.Wait();
            try
            {
                bool shouldPrintResponse = false;
                switch (_state)
                {
                    case ClientState.Start:
                        shouldPrintResponse = HandleReceiveStartState(response);
                        break;
                    case ClientState.Auth:
                        shouldPrintResponse =  await HandleReceiveAuthState(response);
                        break;
                    case ClientState.Open:
                        shouldPrintResponse = await HandleReceiveOpenState(response);
                        break;
                    case ClientState.Join:
                        shouldPrintResponse = HandleReceiveJoinState(response);
                        break;
                    case ClientState.End:
                        Debugger.Log("Reached end state, no responses should be received");
                        break;
                }
                if (shouldPrintResponse)
                {
                    Console.WriteLine(response.ToString());
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool HandleSendStartState(Command command)
        {
            if (command.Type == CommandType.Bye)
            {
                _state = ClientState.End;
                return true;
            }
            else if (command.Type == CommandType.Auth)
            {
                _state = ClientState.Auth;
                return true;
            }
            Console.WriteLine($"ERROR: Must authenticate before sending {command.Type}");
            return false;
        }

        private bool HandleSendAuthState(Command command)
        {
            if (command.Type == CommandType.Bye)
            {
                _state = ClientState.End;
                return true;
            }
            else if (command.Type == CommandType.Auth)
            {
                return true;
            }
            Console.WriteLine("ERROR: Waiting for response from the server");
            return false;
        }

        private bool HandleSendOpenState(Command command)
        {
            if (command.Type == CommandType.Bye)
            {
                _state = ClientState.End;
                return true;
            }
            else if (command.Type == CommandType.Msg)
            {
                return true;
            }
            else if (command.Type == CommandType.Join)
            {
                _state = ClientState.Join;
                return true;
            }
            Console.WriteLine($"ERROR: State Open: Invalid command {command.Type}");
            return false;
        }

        private bool HandleSendJoinState(Command command)
        {
            if (command.Type == CommandType.Bye)
            {
                _state = ClientState.End;
                return true;
            }
            return false;
        }

        private bool HandleReceiveStartState(Response response)
        {
            _state = ClientState.End;
            return true;
        }

        private async Task<bool> HandleReceiveAuthState(Response response)
        {
            if (response.Type == ResponseType.Msg)
            {
                _state = ClientState.End;
                string reply = "Received message in Auth state, terminating connection";
                Console.WriteLine($"ERROR: {reply}");
                Command command = new Command(type: CommandType.Err, content: reply);
                await _client.SendMessageAsync(command);
                return false;
            }
            else if (response.Type == ResponseType.ReplyNok)
            {
                return true;
            }
            else if (response.Type == ResponseType.ReplyOk)
            {
                _state = ClientState.Open;
            }
            else if (response.Type == ResponseType.Err || response.Type == ResponseType.Bye)
            {
                _state = ClientState.End;
            }
            return true;
        }

        private async Task<bool> HandleReceiveOpenState(Response response)
        {
            if (response.Type == ResponseType.ReplyOk || response.Type == ResponseType.ReplyNok)
            {
                _state = ClientState.End;
                string reply = "Received reply in Open state, terminating connection";
                Console.WriteLine($"ERROR: {reply}");
                Command command = new Command(type: CommandType.Err, content: reply);
                await _client.SendMessageAsync(command);
                return false;
            }
            else if (response.Type == ResponseType.Err || response.Type == ResponseType.Bye)
            {
                _state = ClientState.End;
            }
            else if (response.Type == ResponseType.Msg)
            {
                return true;
            }
            return true; 
        }

        private bool HandleReceiveJoinState(Response response)
        {
            if (response.Type == ResponseType.ReplyOk || response.Type == ResponseType.ReplyNok)
            {
                _state = ClientState.Open;
            }
            else if (response.Type == ResponseType.Msg)
            {
                return true;;
            }
            else if (response.Type == ResponseType.Err || response.Type == ResponseType.Bye)
            {
                _state = ClientState.End;
            }
            return true;
        }
    }
}