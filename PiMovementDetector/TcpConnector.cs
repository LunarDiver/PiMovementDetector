using System.Timers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text.Json;

namespace PiMovementDetector
{
    /// <summary>
    /// Provides a TCP subscriber model to write data to one or more TCP clients.
    /// </summary>
    public class TcpConnector : IDisposable
    {
        /// <summary>
        /// Gets the number of TCP clients currently connected to this instance.
        /// </summary>
        public int ConnectedClientsCount
        {
            get
            {
                return _connectedClients.Count;
            }
        }

        /// <summary>
        /// Gets or sets whether to send a <see cref="ushort"/> to inform TCP clients of the size of the incoming data.
        /// </summary>
        public bool WriteLengthIndicator { get; set; }

        /// <summary>
        /// The underlying <see cref="TcpListener"/> to handle all incoming connections.
        /// </summary>
        private readonly TcpListener _listener;

        /// <summary>
        /// Lists all clients that have been connected to this instance.
        /// </summary>
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>();

        /// <summary>
        /// Regularly checks if any new connections are incoming and automatically accepts them.
        /// </summary>
        private readonly Timer _connector = new Timer
        {
            AutoReset = false,
            Interval = 1000
        };

        /// <summary>
        /// Initializes a new <see cref="TcpConnector"/> and auto assigns a port to use for this instance.
        /// </summary>
        /// <param name="port">The TCP port that will be used for this instance.</param>
        /// <param name="writeLength">Enables or disables writing data length prior to sending the actual data.</param>
        public TcpConnector(out ushort port, bool writeLength = false)
        {
            //Creates the underlying listener and auto assigns a port
            _listener = TcpListener.Create(0);
            _listener.Start();

            //Starts checking for incoming connections
            _connector.Elapsed += ConnectNewClient;
            _connector.Start();

            WriteLengthIndicator = writeLength;

            //Gets the port used for this instance
            port = (ushort)((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        /// <summary>
        /// Writes bytes to any clients that may be connected. Doesn't do anything if no client is connected.
        /// </summary>
        /// <param name="buffer">The bytes that should be written to the clients.</param>
        public void Write(params byte[] buffer)
        {
            CheckConnections();

            for (int i = 0; i < _connectedClients.Count; i++)
            {
                var s = _connectedClients[i].GetStream();

                if (WriteLengthIndicator)
                    s.Write(BitConverter.GetBytes((ushort)buffer.Length));

                s.Write(buffer);
            }
        }

        /// <summary>
        /// Writes data to any clients that may be connected. Doesn't do anything if no client is connected.
        /// </summary>
        /// <param name="data">The data that should be written to the clients.</param>
        public void Write<T>(T data)
        {
            if (!typeof(T).IsSerializable)
                throw new ArgumentException($"Type {nameof(T)} must be serializable.");

            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes<T>(data);

            Write(serialized);
        }

        /// <summary>
        /// Asynchronously writes bytes to any clients that may be connected. Doesn't do anything if no client is connected.
        /// </summary>
        /// <param name="buffer">The bytes that should be written to the clients.</param>
        public async Task WriteAsync(params byte[] buffer)
        {
            CheckConnections();

            for (int i = 0; i < _connectedClients.Count; i++)
            {
                var s = _connectedClients[i].GetStream();

                if (WriteLengthIndicator)
                    await s.WriteAsync(BitConverter.GetBytes((ushort)buffer.Length));

                await s.WriteAsync(buffer);
            }
        }

        /// <summary>
        /// Asynchronously writes data to any clients that may be connected. Doesn't do anything if no client is connected.
        /// </summary>
        /// <param name="data">The data that should be written to the clients.</param>
        public async Task WriteAsync<T>(T data)
        {
            if (!typeof(T).IsSerializable)
                throw new ArgumentException($"Type {nameof(T)} must be serializable.");

            byte[] serialized = JsonSerializer.SerializeToUtf8Bytes<T>(data);

            await WriteAsync(serialized);
        }

        /// <summary>
        /// This event will be called every time a new client is connected and returns its <see cref="NetworkStream"/>.
        /// </summary>
        public event EventHandler<NetworkStream> ClientConnected;

        /// <summary>
        /// Waits for any incoming TCP connections and accepts the next one.
        /// </summary>
        private void ConnectNewClient(object sender, ElapsedEventArgs e)
        {
            _listener.BeginAcceptTcpClient(ClientAccepted, null);
        }

        /// <summary>
        /// Adds the accepted client to the list of connected clients and restarts <see cref="_connector"/>.
        /// </summary>
        private void ClientAccepted(IAsyncResult ar)
        {
            TcpClient client = _listener.EndAcceptTcpClient(ar);
            _connectedClients.Add(client);
            ClientConnected?.Invoke(this, client.GetStream());

            _connector.Start();
        }

        /// <summary>
        /// Checks if any added clients are no longer connected and removes them.
        /// </summary>
        private void CheckConnections()
        {
            for (int i = 0; i < _connectedClients.Count; i++)
            {
                var currClient = _connectedClients[i];
                if (!currClient.Connected)
                {
                    currClient.Dispose();
                    _connectedClients.Remove(currClient);
                    i--;
                    continue;
                }
            }
        }

        public void Dispose()
        {
            _connector.Dispose();
            _listener.Stop();

            foreach (TcpClient client in _connectedClients)
                client.Dispose();
        }
    }
}