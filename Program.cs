namespace LNLNatPunch
{
    internal class Program
    {
        private NetPeer _host;
        private NetPeer _client;
        private Relay _relay;

        static void Main(string[] args)
        {
            Program program = new Program();

            program.RunCommands();
        }

        private void RunCommands()
        {
            Console.WriteLine("1. Run NAT Relay Server");
            Console.WriteLine("2. Run Server");
            Console.WriteLine("3. Run Client");

            string input = Console.ReadLine();

            if (!int.TryParse(input, out int result))
            {
                Console.WriteLine("Invalid format");
                return;
            }

            if (result == 1)
            {
                RunRelay();
                return;
            }

            if (result == 2)
            {
                RunHost();
                return;
            }

            if (result == 3)
            {
                RunClient();
                return;
            }
        }

        public void RunRelay()
        {
            _relay = new Relay();
            _relay.Init();

            PollUpdate();
        }

        private void RunHost()
        {
            Console.WriteLine("Please input the NAT punch relay address:");
            string inputNatRelayAddress = Console.ReadLine();

            _host = new NetPeer();
            _host.Init(true);
            _host.NatPunch(inputNatRelayAddress, 9_000, "helloworld");
            
            PollUpdate();
        }

        private void RunClient()
        {
            Console.WriteLine("Please input the host endpoint:");
            string inputHostEndPoint = Console.ReadLine();

            Console.WriteLine("Please input the NAT punch relay address:");
            string inputNatRelayAddress = Console.ReadLine();

            _client = new NetPeer();
            _client.Init(false);
            _client.NatPunch(inputNatRelayAddress, 9_000, inputHostEndPoint);
            
            PollUpdate();
        }

        private void PollUpdate()
        {
            while (true)
            {
                _relay?.PollUpdate();
                _host?.PollUpdate();
                _client?.PollUpdate();
                Thread.Sleep(10);
            }
        }
    }
}
