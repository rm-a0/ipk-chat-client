class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var parser = new ArgumentParser();
            parser.Parse(args);

            Debugger.Enable(parser.Debug);
            Debugger.Log("Debugger enabled");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Parsing arguments failed: {ex.Message}");
        }
    }
}