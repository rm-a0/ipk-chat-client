class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var parser = new ArgumentParser();
            parser.Parse(args);
            var chatApp = new ChatApplication(parser);
            await chatApp.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }

    }
}