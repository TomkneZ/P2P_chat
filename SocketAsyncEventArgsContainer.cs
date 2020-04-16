using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
namespace P2P_Chat_on_Sockets
{
    public class SocketAsyncEventArgsContainer
    {
        public byte[] buffer { get; }
        public SocketError errorStatus {get; }
        public IPEndPoint peerIP { get; }
        public string peerName { get; }
        public SocketAsyncEventArgsContainer(byte[] buffer, int start, int length, SocketError error, IPEndPoint IP, string username)
        {
            this.buffer = new byte[length];
            Array.Copy(buffer, start, this.buffer, 0, length);
            this.errorStatus = error;
            this.peerIP = IP;
            this.peerName = username;
        }
    }
}
