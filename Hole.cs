using System.Net;
using LiteNetLib;

namespace LNLNatPunch
{
    public class NATPeer
    {
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public DateTime LastHeartbeatTime { get; private set; }

        public void Refresh()
        {
            LastHeartbeatTime = DateTime.UtcNow;
        }

        public NATPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }

    public struct NATPeerPair
    {
        public string Token;
        public NATPeer Peer;
    }

    class Relay : INatPunchListener
    {
        private NetManager _netManager;
        private EventBasedNetListener _listener;
        private int _listenPort = 9000;

        private readonly List<NATPeerPair> _activePeers = new List<NATPeerPair>();

        public void Init()
        {
            _netManager = new NetManager(_listener)
            {
                NatPunchEnabled = true,
            };
            _netManager.Start(_listenPort);
            _netManager.NatPunchModule.Init(this);

            Console.WriteLine($"NAT Punch relay is started at: {_listenPort}");
        }

        public void PollUpdate()
        {
            _netManager.NatPunchModule.PollEvents();
            _netManager.PollEvents();

            DateTime timeNow = DateTime.UtcNow;

            for (int i = _activePeers.Count - 1; i >= 0; i--)
            {
                //NATPeerPair peerPair = _activePeers[i];
                //NATPeer peer = peerPair.Peer;

                //DateTime expiredTime = peer.LastHeartbeatTime.AddSeconds(10);
                //bool isPeerExpired = timeNow > expiredTime;

                //if (isPeerExpired)
                //{
                //    _activePeers.RemoveAt(i);
                //}
            }
        }

        private bool TryGetPeerPair(string token, out int index)
        {
            for (int i = 0; i < _activePeers.Count; i++)
            {
                NATPeerPair peerPair = _activePeers[i];

                if (peerPair.Token == token)
                {
                    index = 0;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            Console.WriteLine($"[Relay]: A Nat introduction request is received by: {localEndPoint} {remoteEndPoint}");

            bool hasPool = TryGetPeerPair(token, out int index);

            if (hasPool)
            {
                NATPeerPair pair = _activePeers[index];
                NATPeer clientIntroduction = pair.Peer;

                Console.WriteLine($"[Relay]: A matching peer has been found, sending to the clients");
                _netManager.NatPunchModule.NatIntroduce(clientIntroduction.InternalAddr, clientIntroduction.ExternalAddr, localEndPoint, remoteEndPoint, token);
                return;
            }

            NATPeer newCandidate = new(localEndPoint, remoteEndPoint);
            _activePeers.Add(new NATPeerPair() { Token = remoteEndPoint.ToString(), Peer = newCandidate });
            Console.WriteLine($"[Relay]: A host is added: {remoteEndPoint}");
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
        }
    }

    class NetPeer : INatPunchListener
    {
        private NetManager _netManager;
        private EventBasedNetListener _listener;
        private bool _isServer;

        public void Init(bool runAsServer)
        {
            _isServer = runAsServer;

            _listener = new EventBasedNetListener();
            _listener.PeerConnectedEvent += PeerConnectedEvent;

            _netManager = new NetManager(_listener)
            {
                NatPunchEnabled = true,
            };

            if (runAsServer)
            {
                Console.WriteLine("Listening at port 7777");
                _netManager.Start(7777);
                _listener.ConnectionRequestEvent += ConnectionRequestEvent;
            }
            else
                _netManager.Start();

            _netManager.NatPunchModule.Init(this);
        }

        private void PeerConnectedEvent(LiteNetLib.NetPeer peer)
        {
            Console.WriteLine($"[NetPeer_{_isServer}]: Peer is connected!");
        }

        private void ConnectionRequestEvent(ConnectionRequest request)
        {
            request.Accept();
        }

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            Console.WriteLine($"[NetPeer_{_isServer}]: A Nat introduction request is received!");
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            Console.WriteLine($"[NetPeer_{_isServer}]: A Nat introduction is success! found a peer! {targetEndPoint} NatAddressType: {type} token: {token}");

            if (!_isServer)
            {
                Console.WriteLine($"[NetPeer_{_isServer}]: Connecting to server now...");
                _netManager.Connect(targetEndPoint, string.Empty);
            }
        }

        public void PollUpdate()
        {
            _netManager.NatPunchModule.PollEvents();
            _netManager.PollEvents();
        }

        public void NatPunch(string relayAddress, int relayPort, string hostEndPoint)
        {
            _netManager.NatPunchModule.SendNatIntroduceRequest(relayAddress, relayPort, hostEndPoint);
        }
    }

    public class Fullstack
    {
        private NetPeer _host;
        private NetPeer _client;
        private Relay _relay;

        public void Run()
        {
            _relay = new Relay();
            _host = new NetPeer();
            _client = new NetPeer();

            _relay.Init();

            _host.Init(true);
            _client.Init(false);

            _host.NatPunch("localhost", 9_000, "helloworld");
            _client.NatPunch("localhost", 9_000, "helloworld");

            while (true)
            {
                _relay.PollUpdate();
                _host.PollUpdate();
                _client.PollUpdate();
                Thread.Sleep(10);
            }
        }
    }
}
