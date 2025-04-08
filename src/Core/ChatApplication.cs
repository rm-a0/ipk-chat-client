namespace Ipk25Chat.Core
{
    public class ChatApplication
    {
        private readonly ArgumentParser _parser;
        private readonly InputParser _inputParser;
        private ChatClient? _client;
        private ChatStateMachine? _stateMachine;
        private Task? _listenerTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ChatApplication(ArgumentParser parser)
        {
            _parser = parser;
            _inputParser = new InputParser();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task RunAsync()
        {
            try
            {
                await InitializeAsync();
                await ReadUserInputAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private async Task InitializeAsync()
        {
            Debugger.Enable(_parser.Debug);
            Debugger.Log("Debugger enabled");
            Debugger.Log($"Server IP: {_parser.Server}");

            _client = ChatClientFactory.Create(_parser.Protocol, _parser.Server, _parser.Port);
            await _client.ConnectAsync();

            _stateMachine = new ChatStateMachine(_client);
            _stateMachine.RequestExit += OnStateMachineRequestExit;

            // Start the server listener as a background task
            _listenerTask = Task.Run(() => ListenToServerAsync(_cancellationTokenSource.Token));
        }

        private async Task ListenToServerAsync(CancellationToken token)
        {
            await _client!.ListenToServerAsync(_stateMachine!, token);
            Debugger.Log("Listening to server finished");
        }

        private async Task ReadUserInputAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var inputTask = Task.Run(() => Console.ReadLine());
            var completed = await Task.WhenAny(inputTask, Task.Delay(-1, _cancellationTokenSource.Token));

            if (completed == inputTask)
            {
                string? input = inputTask.Result;
                if (input == null) 
                    break;
                Debugger.Log($"User input from console: {input}");
                await ProcessInputAsync(input);
            }
            else
            {
                break;
            }
        }
        }

        private async Task ProcessInputAsync(string input)
        {
            try
            {
                Command command = _inputParser.Parse(input);
                if (!command.IsLocal && command.Type != CommandType.Unknown)
                {
                    await _stateMachine!.HandleCommandAsync(command);
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private async Task HandleExitAsync()
        {
            _cancellationTokenSource.Cancel();
            if (_client != null)
            {
                await _client.DisconnectAsync();
            }
        }

        private void OnStateMachineRequestExit(object? sender, EventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task CleanupAsync()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
            }

            if (_listenerTask != null)
            {
                await _listenerTask;
            }
            Debugger.Log("Clean exit");
        }
    }
}