using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        var parser = new ArgumentParser();
        var inputParser = new InputParser();
        ChatClient? client = null;
        ChatStateMachine? stateMachine = null;
        Task? listenerTask = null;

        try
        {
            parser.Parse(args);

            Debugger.Enable(parser.Debug);
            Debugger.Log("Debugger enabled");
            Debugger.Log($"Server IP: {parser.Server}");

            client = ChatClientFactory.Create(parser.Protocol, parser.Server, parser.Port);
            await client.ConnectAsync();

            stateMachine = new ChatStateMachine(client);

            // Run server listener asynchronously
            listenerTask = Task.Run(async () =>
            {
                try
                {
                    await client.ListenToServerAsync(stateMachine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    
                }
            });

            string? input;
            while ((input = Console.ReadLine()) != null)
            {
                Debugger.Log($"User input from console: {input}");
                try
                {
                    Command command = inputParser.Parse(input);
                    if (!command.IsLocal && command.Type != CommandType.Unknown)
                    {
                        await stateMachine.HandleCommandAsync(command);
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
        finally {
            if (client != null)
            {
                await client.DisconnectAsync();
            }

            if (listenerTask != null)
            {
                await listenerTask;
            }

            Debugger.Log("Clean exit");
        }
    }
}