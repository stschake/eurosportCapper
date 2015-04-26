using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace eurosportCapper
{

    public class VLCClient
    {
        private const string Response =
            "HTTP/1.1 200 OK\r\nConnection: keep-alive\r\nContent-Type: video/mp2t\r\n\r\n";

        private readonly byte[] _responseData;
        private static readonly byte[] Pattern = {(byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n'};

        private readonly byte[] _buffer = new byte[128];
        private int _seen = 0;
        private Socket RemoteSocket { get; set; }
        public bool AcceptingData { get; private set; }

        public event EventHandler OnDisconnect;

        private readonly List<StreamDataEvent> _packetQueue = new List<StreamDataEvent>();
        private int _lastSentPart = -1;
        private DateTime _timeSinceLastPart = DateTime.Now;

        public VLCClient(Socket remote)
        {
            _responseData = Encoding.ASCII.GetBytes(Response);
            RemoteSocket = remote;
            AcceptingData = false;
            remote.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, HandleReceive, null);
        }

        private void HandleReceive(IAsyncResult ar)
        {
            try
            {
                int r = RemoteSocket.EndReceive(ar);
                for (int i = 0; i < r; i++)
                {
                    if (_buffer[i] != Pattern[_seen])
                        _seen = 0;
                    else
                        _seen++;

                    if (_seen == Pattern.Length)
                    {
                        RemoteSocket.Send(_responseData);
                        AcceptingData = true;
                        break;
                    }
                }

                if (!AcceptingData)
                    RemoteSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, HandleReceive, null);
            }
            catch (Exception)
            {
                if (OnDisconnect != null)
                    OnDisconnect(this, null);
            }
        }

        public void Relay(StreamDataEvent dataEvent)
        {
            if (_lastSentPart != -1 && _lastSentPart >= dataEvent.PartNumber)
            {
                CheckSendNext();
                return;
            }
            if (_packetQueue.Any(p => p.PartNumber == dataEvent.PartNumber))
            {
                CheckSendNext();
                return;
            }

            Console.WriteLine("Queueing " + dataEvent.PartNumber);

            int i = 0;
            for (; i < _packetQueue.Count; i++)
            {
                if (dataEvent.PartNumber < _packetQueue[i].PartNumber)
                {
                    _packetQueue.Insert(i, dataEvent);
                    break;
                }
            }
            if (i == _packetQueue.Count)
                _packetQueue.Add(dataEvent);

            CheckSendNext();
        }

        private void CheckSendNext()
        {
            lock (_packetQueue)
            {
                if (_packetQueue.Count == 0)
                    return;
                if (_packetQueue.Count >= 4 || (_lastSentPart == (_packetQueue[0].PartNumber - 1)) ||
                    ((DateTime.Now - _timeSinceLastPart).TotalSeconds > 8 && _lastSentPart != -1))
                {
                    Console.WriteLine("Sending " + _packetQueue[0].PartNumber);
                    SendNext();
                }
            }
        }

        private void SendNext()
        {
            try
            {
                RemoteSocket.BeginSend(_packetQueue[0].Data, 0, _packetQueue[0].Data.Length, SocketFlags.None,
                    HandleSend, _packetQueue[0]);
                _lastSentPart = _packetQueue[0].PartNumber;
                _timeSinceLastPart = DateTime.Now;
                _packetQueue.RemoveAt(0);

                ThreadPool.QueueUserWorkItem(ignore =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(9));
                    CheckSendNext();
                });
            }
            catch (Exception)
            {
                if (OnDisconnect != null)
                    OnDisconnect(this, null);
            }
        }

        private void HandleSend(IAsyncResult ar)
        {
            try
            {
                var buffer = (StreamDataEvent) ar.AsyncState;
                RemoteSocket.EndSend(ar);
            }
            catch (Exception)
            {
                if (OnDisconnect != null)
                    OnDisconnect(this, null);
            }
        }
    }

    public class VLCRelay
    {
        private readonly Socket _listener;
        private readonly List<VLCClient> _clients = new List<VLCClient>(); 

        public VLCRelay(int port)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, port));
            _listener.Listen(5);
            _listener.BeginAccept(HandleAccept, null);
            Console.WriteLine("VLCRelay listening on port " + port);
        }

        private void HandleAccept(IAsyncResult ar)
        {
            Socket remote = _listener.EndAccept(ar);
            Console.WriteLine("VLCRelay received connection");
            lock (_clients)
            {
                var newClient = new VLCClient(remote);
                newClient.OnDisconnect += HandleDisconnect;
                _clients.Add(newClient);
            }
            _listener.BeginAccept(HandleAccept, null);
        }

        public void Relay(StreamDataEvent ev)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client.AcceptingData)
                        client.Relay(ev);
                }
            }
        }

        private void HandleDisconnect(object sender, EventArgs e)
        {
            lock (_clients)
                _clients.Remove((VLCClient) sender);
        }
    }

}