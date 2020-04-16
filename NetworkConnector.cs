using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;


namespace P2P_Chat_on_Sockets
{
    class NetworkConnector
    {
        public const int MAXNAMELEN = 255;

        public const int UDPPORT = 5358;
        public const int TCPPORT = 5359;

        public const int MAXPENDINGCLIENTS = 8;

        static public string myUsername { get; set; }

        static private Socket UDPStartSocket;
        static private Socket TCPListenSocket;

        static private IPEndPoint localUDPEndPoint { get; }
        static private IPEndPoint localTCPEndPoint { get; }

        static private SocketAsyncEventArgs asyncUDPRecvEventArgs;
        static private SocketAsyncEventArgs asyncTCPAcceptEventArgs;

        static NetworkConnector()
        {
            UDPStartSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            TCPListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            UDPStartSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

            localUDPEndPoint = new IPEndPoint(IPAddress.Any, UDPPORT);
            localTCPEndPoint = new IPEndPoint(IPAddress.Any, TCPPORT);

                      
            asyncUDPRecvEventArgs = new SocketAsyncEventArgs();
            byte[] buf = new byte[255];
            asyncUDPRecvEventArgs.SetBuffer(buf, 0, MAXNAMELEN);
            asyncUDPRecvEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, UDPPORT);
            asyncUDPRecvEventArgs.Completed += EstablishTCPConnection;

            //чтобы установить TCP подключение
            //Минимальный требуемый размер буфера 288(?)
            //если требуются дополнительные данные определенного объема, то размер буфера должен быть минимальным размером буфера и этим значением
            asyncTCPAcceptEventArgs = new SocketAsyncEventArgs();
            asyncTCPAcceptEventArgs.Completed += AcceptTCPConnection;
            asyncTCPAcceptEventArgs.SetBuffer(new byte[288 + MAXNAMELEN], 0, MAXNAMELEN);

            UDPStartSocket.Bind(localUDPEndPoint);
            TCPListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TCPListenSocket.Bind(localTCPEndPoint);
            TCPListenSocket.Listen(MAXPENDINGCLIENTS);
        }
        static public void SendStartupMsg()
        {
            UDPStartSocket.SendTo(Encoding.ASCII.GetBytes(myUsername), new IPEndPoint(IPAddress.Broadcast, UDPPORT));
        }
        
        static public void ListenForUDPRequests(ChatForm source)
        {
            asyncUDPRecvEventArgs.UserToken = source;
            while (!UDPStartSocket.ReceiveFromAsync(asyncUDPRecvEventArgs))
            {
                EstablishTCPConnectionSync(UDPStartSocket, asyncUDPRecvEventArgs);
            }
        }
        static public void EstablishTCPConnection(object Sender, SocketAsyncEventArgs e)
        {
            EstablishTCPConnectionSync(Sender, e);
            ListenForUDPRequests((ChatForm)e.UserToken);
        }
        static public void EstablishTCPConnectionSync(object Sender, SocketAsyncEventArgs e)
        {
            string peerName = Encoding.ASCII.GetString(e.Buffer, e.Offset, e.BytesTransferred);
            if (!peerName.Equals(myUsername))
            {
                var connection = new Peer((ChatForm)e.UserToken,
                    new IPEndPoint(((IPEndPoint)e.RemoteEndPoint).Address, TCPPORT), peerName);
                // проверяем, чтобы пира не было в списке
                if (!((ChatForm)e.UserToken).ContainsPeer(connection))
                {
                    connection.EstablishPeerConnection(localTCPEndPoint);
                }
                else
                {
                    connection.Close();
                }
            }
        }

        static public void ListenForConnections(ChatForm source)
        {
            var TCPAcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            TCPAcceptSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            asyncTCPAcceptEventArgs.AcceptSocket = TCPAcceptSocket;
            asyncTCPAcceptEventArgs.UserToken = source;
            while (!TCPListenSocket.AcceptAsync(asyncTCPAcceptEventArgs))
            {
                AcceptTCPConnectionSync(TCPListenSocket, asyncTCPAcceptEventArgs);
            }
        }
        static public void AcceptTCPConnection (object Sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AcceptTCPConnection(Sender, e);
            }
            ListenForConnections((ChatForm)e.UserToken);
        }
        static public void AcceptTCPConnectionSync (object Sender, SocketAsyncEventArgs e)
        {
            var acceptedConnection = new Peer((ChatForm)e.UserToken,
                   Encoding.ASCII.GetString(e.Buffer, e.Offset, e.BytesTransferred).TrimEnd('\0'), e.AcceptSocket);

            if (!((ChatForm)e.UserToken).ContainsPeer(acceptedConnection))
            {
                var temp = (ChatForm)e.UserToken;
                e.UserToken = acceptedConnection;
                temp.ConnectionEstablished(Sender, e);
                e.UserToken = temp;
            }
            else
            {
                acceptedConnection.Close();
            }

        }
    }
}
