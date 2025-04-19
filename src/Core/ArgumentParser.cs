using System.Net;
using System.Net.Sockets;

namespace Ipk25Chat.Core
{
    public class ArgumentParser
    {
        public string Protocol { get; private set; } = string.Empty;
        public string Server { get; private set; } = string.Empty;
        public int Port { get; private set; } = 4567;
        public int UdpTimeout { get; private set; } = 250;
        public int UdpRetries { get; private set; } = 3;
        public bool Debug { get; private set; } = false;

        public void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-t":
                    case "--protocol":
                        Protocol = GetNextArg(args, ref i).ToLower();
                        if (Protocol != "tcp" && Protocol != "udp")
                            throw new ArgumentException("Protocol must be 'tcp' or 'udp'");
                        break;

                    case "-s":
                    case "--server":
                        string? serverIp = HostnameToIp(GetNextArg(args, ref i)!);
                        if (string.IsNullOrEmpty(serverIp))
                        {
                            throw new ArgumentException("Invalid IP address or hostname");
                        }
                        Server = serverIp;
                        break;

                    case "-p":
                    case "--port":
                        Port = int.Parse(GetNextArg(args, ref i));
                        if (Port < 1 || Port > 65535)
                            throw new ArgumentException("Port must be 1-65535");
                        break;

                    case "-d":
                    case "--timeout":
                        UdpTimeout = int.Parse(GetNextArg(args, ref i));
                        break;

                    case "-r":
                    case "--retries":
                        UdpRetries = int.Parse(GetNextArg(args, ref i));
                        break;

                    case "-dbg":
                    case "--debug":
                        Debug = true;
                        break;

                    case "-h":
                    case "--help":
                        PrintHelp();
                        Environment.Exit(0);
                        break;

                    default:
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                }
            }

            if (string.IsNullOrEmpty(Protocol) || string.IsNullOrEmpty(Server))
                throw new ArgumentException("Mandatory arguments -t (tcp/udp) and -s are required.");
        }

        private string GetNextArg(string[] args, ref int i)
        {
            if (++i >= args.Length)
                throw new ArgumentException($"Missing value for argument: {args[i - 1]}");
            return args[i];
        }

        private void PrintHelp()
        {
            Console.WriteLine("Usage: ipk25chat-client [OPTIONS]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -t, --protocol <tcp|udp>  Transport protocol (required)");
            Console.WriteLine("  -s, --server <host>       Server hostname/IP (required)");
            Console.WriteLine("  -p, --port <number>       Server port (default: 4567)");
            Console.WriteLine("  -d, --timeout <ms>        UDP confirmation timeout (default: 250)");
            Console.WriteLine("  -r, --retries <count>     UDP max retransmissions (default: 3)");
            Console.WriteLine("  -dbg, --debug             Enable debug logging");
            Console.WriteLine("  -h, --help                Show this help");
        }

        // Source: Grok3 (Entierly generated)
        private string? HostnameToIp(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                return null;

            if (IPAddress.TryParse(hostname, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return hostname;
            }
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(hostname);
                
                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }
            }
            catch (SocketException)
            {
                return null;
            }
            return null;
        }
    }
}