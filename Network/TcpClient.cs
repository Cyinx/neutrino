using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace neutrino {
    class TcpClient : TcpConn, NetworkComponent {
        public TcpClient(NetworkMgr networkMgr, params Option[] opts) :
            base(new Socket(SocketType.Stream, ProtocolType.Tcp), networkMgr) {
            try {
                foreach (var x in opts)
                    x.Do(this);
            } catch (Exception ex) {
                sLog.Debug("tcp_server", "init tcp server error {1}", ex.Message);
            }
        }

       void NetworkComponent.Start() {
            tcpSocket.Connect(this.remoteAddress, remotePort);
            base.Start();
        }

        public override void Start() {
            ((NetworkComponent)this).Start();
        }
        public override void Close() {
            tcpSocket.Close();
        }

        public string Name() {
            return sName;
        }

        internal string remoteAddress;
        internal ushort remotePort;
        internal string sName;
    }
}
