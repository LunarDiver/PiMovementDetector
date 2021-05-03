using System;
using System.Net;
using System.Net.Sockets;

namespace PiMovementDetector
{
    public class TcpConnector : IDisposable
    {
        public bool Connected { get; private set; }

        private readonly TcpListener _listener;

        private TcpClient _connectedClient;

        public TcpConnector(out ushort port)
        {
            _listener = TcpListener.Create(0);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientAccepted, null);

            port = (ushort)((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public event EventHandler<NetworkStream> ClientConnected;

        private void ClientAccepted(IAsyncResult ar)
        {
            _connectedClient = _listener.EndAcceptTcpClient(ar);
            Connected = true;
            ClientConnected.Invoke(this, _connectedClient.GetStream());
        }

        public void Dispose()
        {
            _listener.Stop();
            _connectedClient?.Dispose();
        }
    }
}