using System.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PiMovementDetector.Tests
{
    [TestClass]
    public class CommunicationTests
    {
        [TestMethod]
        public void IsConnectionPossible()
        {
            using var connector = new TcpConnector(out ushort port);
            connector.ClientConnected += Continue;
            using var connectingClient = new TcpClient();
            connectingClient.Connect(IPAddress.Any, port);

            bool success = false;

            void Continue(object sender, NetworkStream e)
            {
                success = true;
            }

            int tries = 0;
            int totalTries = 30;
            while (!success && tries < totalTries)
            {
                Thread.Sleep(100);
            }

            Assert.AreNotEqual(totalTries, tries);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void CanReadWriteData()
        {
            TcpClient connectingClient = null;
            using var connector = new TcpConnector(out ushort port);
            connector.ClientConnected += Continue;
            connectingClient = new TcpClient();
            connectingClient.Connect(IPAddress.Any, port);

            bool finished = false;
            int? receivedAtClient = default;
            int receivedAtListener = default;

            void Continue(object sender, NetworkStream e)
            {
                //Write to client / Read from client
                e.WriteByte(123);
                receivedAtClient = connectingClient?.GetStream().ReadByte();

                //Write to listener / Read from listener
                connectingClient?.GetStream().WriteByte(213);
                receivedAtListener = e.ReadByte();

                finished = true;
            }

            while (!finished)
            {
                Thread.Sleep(10);
            }

            Assert.AreEqual(123, receivedAtClient);
            Assert.AreEqual(213, receivedAtListener);

            connectingClient?.Dispose();
        }
    }
}