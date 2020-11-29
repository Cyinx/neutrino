using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace neutrino {
    public partial class TcpConn : NetworkConn {
        public TcpConn(Socket s, NetworkMgr mgr) {
            this.agentID = AgentID.Gen();

            tcpSocket = s;
            networkMgr = mgr;

            writeEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(DoEventComplete);
            readEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(DoEventComplete);
        }

        public AgentID GetID() {
            return agentID;
        }

        public virtual void Start() {
            TransportManager.AddNetworkConn(this);
        }

        public virtual void Close() {
            tcpSocket.Close();
        }

        private void StartClose(NetworkCloseState state) {
            TransportManager.CloseNetworkConn(this);
        }

        private void DoEventComplete(object sender, SocketAsyncEventArgs e) {
            switch (e.LastOperation) {
                case SocketAsyncOperation.Receive:
                    OnSocketRead(e);
                    break;
                case SocketAsyncOperation.Send:
                    OnSocketSend(e);
                    break;
                default:
                    break;
            }
        }

        protected Socket tcpSocket;
        SocketAsyncEventArgs writeEventArg = new SocketAsyncEventArgs();
        SocketAsyncEventArgs readEventArg = new SocketAsyncEventArgs();
        AgentID agentID;
        NetworkMgr networkMgr;
    }
}
