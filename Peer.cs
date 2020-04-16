using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Chat_on_Sockets
{
    public class Peer
    {
        public Socket connectionSocket { get; }
        public string peerName { get; }
        public IPEndPoint peerIP { get; }
        public ChatForm peerForm { get; }
        public byte[] sendBuffer;
        public byte[] receiveBuffer;
        public SocketAsyncEventArgs receiveEventArgs;
        public SocketAsyncEventArgs sendEventArgs;

        public Peer(ChatForm source, IPEndPoint destination, string username)
        {
            peerForm = source;
            connectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            peerIP = destination;
            peerName = username;
            sendBuffer = new byte[1024];
            receiveBuffer = new byte[1024];

            receiveEventArgs = new SocketAsyncEventArgs();
            receiveEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            receiveEventArgs.Completed += peerForm.MessageReceived;

            sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            sendEventArgs.Completed += peerForm.MessageSent;
        }

        public Peer(ChatForm source, string username, Socket socket)
        {
            peerForm = source;
            connectionSocket = socket;
            peerIP = socket.RemoteEndPoint as IPEndPoint;
            peerName = username;
            sendBuffer = new byte[1024];
            receiveBuffer = new byte[1024];

            receiveEventArgs = new SocketAsyncEventArgs();
            receiveEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            receiveEventArgs.Completed += peerForm.MessageReceived;

            sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            sendEventArgs.Completed += peerForm.MessageSent;

        }
        public void EstablishPeerConnection(IPEndPoint localEP)
        {
            connectionSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            connectionSocket.Bind(localEP);
            byte[] buf = new byte[255];
            var asyncTCPConnectionEventArgs = new SocketAsyncEventArgs();
            asyncTCPConnectionEventArgs.SetBuffer(buf, 0, NetworkConnector.MAXNAMELEN);
            asyncTCPConnectionEventArgs.RemoteEndPoint = peerIP;
            asyncTCPConnectionEventArgs.Completed += peerForm.ConnectionEstablished;
            asyncTCPConnectionEventArgs.UserToken = this;

            if (!connectionSocket.ConnectAsync(asyncTCPConnectionEventArgs))
            {
                peerForm.ConnectionEstablished(connectionSocket, asyncTCPConnectionEventArgs);
            }
        }
        public void ReceiveMessage()
        {
            receiveEventArgs.UserToken = this;
            bool res = true;
            while (res && !connectionSocket.ReceiveAsync(receiveEventArgs))
            {
                res = peerForm.MessageReceivedSync(connectionSocket, receiveEventArgs);
            }
        }
        public void SendMessage(string message)
        {
            sendEventArgs.UserToken = this;
            Encoding.ASCII.GetBytes(message, 0, message.Length, sendBuffer, 0);

            while (!connectionSocket.SendAsync(sendEventArgs))
            {
                peerForm.MessageSent(connectionSocket, sendEventArgs);
            }
        }
        public void Close()
        {
            if (connectionSocket.Connected)
            {
                // отключаем отправку и получение данных на сокете
                connectionSocket.Shutdown(SocketShutdown.Both); 
            }
            receiveEventArgs.Dispose();
            sendEventArgs.Dispose();
            // закрываем сокет
            connectionSocket.Close();
        }
    }
}
