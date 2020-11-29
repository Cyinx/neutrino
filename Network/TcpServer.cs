using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace neutrino {
    class TcpServer : NetworkComponent {

        public TcpServer(NetworkMgr networkMgr, params Option[] opts) {
            this.networkMgr = networkMgr;
            sAgentID = AgentID.Gen();
            try {
                foreach (var x in opts)
                    x.Do(this);
            } catch (Exception ex) {
                sLog.Debug("tcp_server", "init tcp server error {1}", ex.Message);
            }
        }

        protected void OnAccepted(IAsyncResult ar) {
            try {
                if (sListen == null)
                    return;
                Socket newSocket = sListen.EndAccept(ar);
                TcpConn conn = new TcpConn(newSocket, this.networkMgr);
                conn.Start();
                sListen.BeginAccept(OnAccepted, sListen);
            } catch (Exception ex) {
                sLog.Debug("tcp_server", "on tcp accept error {1}", ex.Message);
            }
        }

        public string Name() {
            return sName;
        }

        public void Close() {
            sListen.Close();
        }

        public AgentID GetID() {
            return this.sAgentID;   
        }

        public void Start() {

            try {
                IPAddress ipAddress = IPAddress.Parse(this.sAddress);
                sListen = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, sPort);
                sListen.Bind(ipEndPoint);
                sListen.Listen(sPort);
                sListen.BeginAccept(OnAccepted, sListen);
            } catch (Exception ex) {
                sLog.Debug("tcp_server", "on tcp server start error {0}", ex.Message);
            }
        }

        AgentID sAgentID;
        internal string sName;
        internal Socket sListen;
        internal string sAddress;
        internal ushort sPort;
        internal NetworkMgr networkMgr;
    }
}
