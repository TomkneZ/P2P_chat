using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace P2P_Chat_on_Sockets
{
    public partial class ChatForm : Form
    {
        public ChatForm()
        {
            InitializeComponent();
        }
        private const string TextBoxNL = "\r\n";

        static private LinkedList<Peer> peers = new LinkedList<Peer>();
        private delegate void SocketEventDelegate(object Sender, SocketAsyncEventArgs e);
        private delegate void ShowSocketEventInfo(SocketAsyncEventArgsContainer messageInfo);

        private void ChatForm_Shown(object sender, EventArgs e)
        {
            var usernameInput = new UsernameForm();
            if (usernameInput.ShowDialog(this) == DialogResult.OK)
            {
                NetworkConnector.myUsername = usernameInput.tbUsername.Text;
            }
            else
            {
                Application.Exit();
                return;
            }
            usernameInput.Dispose();
            StartUpInitiate();

        }
        void StartUpInitiate()
        {
            try
            {
                NetworkConnector.SendStartupMsg();
                tbMessages.AppendText("UDP request was sent" + TextBoxNL);
            }
            catch (SocketException exc)
            {
                MessageBox.Show(this, "Something went wrong while sending UDP broadcast message:" + TextBoxNL + exc.ToString());
                Application.Exit();
                return;
            }
            NetworkConnector.ListenForUDPRequests(this);
            NetworkConnector.ListenForConnections(this);
        }
        public void ConnectionEstablished(object Sender, SocketAsyncEventArgs e)
        {
            var connectionMessageInfo = new SocketAsyncEventArgsContainer(e.Buffer,
                    e.Offset, e.BytesTransferred, e.SocketError, ((Peer)e.UserToken).peerIP,
                    ((Peer)e.UserToken).peerName);

            if (this.InvokeRequired)
            {
                this.Invoke(new ShowSocketEventInfo(ShowConnectionMsg), connectionMessageInfo);
            }
            else
            {
                ShowConnectionMsg(connectionMessageInfo);
            }

            if (e.SocketError == SocketError.Success)
            {
                AddPeer((Peer)e.UserToken);
                ((Peer)e.UserToken).ReceiveMessage();
            }
            else
            {
                ((Peer)e.UserToken).Close();
            }
        }
        public void ShowConnectionMsg(SocketAsyncEventArgsContainer messageInfo)
        {
            long connectionStatus = (long)messageInfo.errorStatus;
            if (connectionStatus == 0)
            {
                tbMessages.AppendText($"Connection with {messageInfo.peerName} " +
                    $"({messageInfo.peerIP.Address.ToString()})" +
                    "is established" + TextBoxNL); ; ;

                btnSend.Enabled = true;
            }
            else
            {
                tbMessages.AppendText($"Connection with {messageInfo.peerName} " +
                    $"({messageInfo.peerIP.Address.ToString()}) "
                    + $"has failed with code {connectionStatus}" + TextBoxNL);
            }

        }
        public void MessageReceived(object Sender, SocketAsyncEventArgs e)
        {
            if (MessageReceivedSync(Sender, e))
            {
                ((Peer)e.UserToken).ReceiveMessage();
            }
        }
        public bool MessageReceivedSync(object Sender, SocketAsyncEventArgs e)
        {
            var messageRecievedMessageInfo = new SocketAsyncEventArgsContainer(e.Buffer,
                    e.Offset, e.BytesTransferred, e.SocketError, (IPEndPoint)e.RemoteEndPoint, ((Peer)e.UserToken).peerName);

            if (this.InvokeRequired)
            {
                this.Invoke(new ShowSocketEventInfo(ShowMessageReceivedMessage), messageRecievedMessageInfo);
            }
            else
            {
                ShowMessageReceivedMessage(messageRecievedMessageInfo);
            }

            switch (e.SocketError)
            {
                case (SocketError.Success):
                    if (e.BytesTransferred == 0)
                    {
                        ((Peer)e.UserToken).Close();
                        RemovePeer((Peer)e.UserToken);
                        return false;
                    }
                    break;
                default:
                    ((Peer)e.UserToken).Close();
                    RemovePeer((Peer)e.UserToken);
                    return false;
            }
            return true;
        }
        private void ShowMessageReceivedMessage(SocketAsyncEventArgsContainer messageInfo)
        {
            switch (messageInfo.errorStatus)
            {
                case (SocketError.Success):
                    if (messageInfo.buffer.Length == 0)
                    {
                        tbMessages.AppendText($"{messageInfo.peerName} disconnected!" + TextBoxNL);
                    }
                    else
                    {
                        string recvdMessage = Encoding.ASCII.GetString(messageInfo.buffer).TrimEnd('\0');
                        if (recvdMessage.Length > 0)
                        {
                            tbMessages.AppendText($"{messageInfo.peerName} " + $"({messageInfo.peerIP.Address.ToString()} says:" + TextBoxNL +
                                Encoding.ASCII.GetString(messageInfo.buffer).TrimEnd('\0') + TextBoxNL);
                        }
                    }
                    break;
                case (SocketError.Disconnecting):
                    tbMessages.AppendText($"{messageInfo.peerName} disconnected!" + TextBoxNL);
                    break;
                default:
                    tbMessages.AppendText($"Connection with {messageInfo.peerName} has failed with code " +
                        (long)messageInfo.errorStatus + TextBoxNL);
                    break;
            }

        }
       
        public void AddPeer(Peer newPeer)
        {
            lock (peers)
            {
                peers.AddFirst(newPeer);
            }
        }

        public void RemovePeer(Peer peer)
        {
            lock (peers)
            {
                peers.Remove(peer);
            }
        }

        public bool ContainsPeer(Peer peer)
        {
            lock (peers)
            {
                foreach (Peer p in peers)
                {
                    if (p.Equals(peer))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = tbInput.Text.Trim();
            if (message.Length > 0)
            {
                lock (peers)
                {
                    foreach (Peer p in peers)
                    {
                        p.SendMessage(message);
                    }
                }
                tbMessages.AppendText("You:" + TextBoxNL + message + TextBoxNL);
                tbInput.Text = string.Empty;                
            }

        }
        public void MessageSent (object Sender, SocketAsyncEventArgs e)
        {
            var messageSentMessageInfo = new SocketAsyncEventArgsContainer(e.Buffer,
               e.Offset, e.BytesTransferred, e.SocketError, (IPEndPoint)e.RemoteEndPoint, ((Peer)e.UserToken).peerName);

            if (this.InvokeRequired)
            {
                this.Invoke(new ShowSocketEventInfo(ShowMessageSentMessage), messageSentMessageInfo);
            }
            else
            {
                ShowMessageSentMessage(messageSentMessageInfo);
            }

            switch (e.SocketError)
            {
                case (SocketError.Success):
                    break;
                default:
                    ((Peer)e.UserToken).Close();
                    RemovePeer((Peer)e.UserToken);
                    break;
            }

        }
        private void ShowMessageSentMessage(SocketAsyncEventArgsContainer messageInfo)
        {
            switch (messageInfo.errorStatus)
            {
                case (SocketError.Success):
                    break;
                case (SocketError.Disconnecting):
                    tbMessages.AppendText($"{messageInfo.peerName} disconnected!" + TextBoxNL);
                    break;
                default:
                    tbMessages.AppendText($"Connection with {messageInfo.peerName} has failed with code " +
                        (long)messageInfo.errorStatus + TextBoxNL);
                    break;
            }
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (peers)
            {
                foreach (Peer p in peers)
                {
                    p.Close();
                }
            }
        }
    }
}
