namespace IPK25Chat;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var parser = new ArgumentParser();
            parser.Parse(args);

            Console.WriteLine($"Protocol: {parser.Protocol}, Server: {parser.Server}, Port: {parser.Port}, " +
                              $"Timeout: {parser.UdpTimeout}, Retries: {parser.UdpRetries}, Verbose: {parser.Verbose}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
    }
}