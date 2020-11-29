using System;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    public interface Option {
        internal void Do(TcpServer s);
        internal void Do(TcpClient c);
    }
    public class NetworkAddr : Option {
        string addr;
        int port;
        public NetworkAddr(string networkAddr) {
            string[] addrs = networkAddr.Split(":");
            if (addrs.Length < 2) {
                throw new Exception(string.Format("Network Option NetworkAddr [{1}] SplitError", networkAddr));
            }
            addr = addrs[0];
            port = Convert.ToInt32(addrs[1]);
        }
        void Option.Do(TcpServer s) {
            s.sPort = (ushort)this.port;
            s.sAddress = this.addr;
        }

        void Option.Do(TcpClient c) {
            c.remotePort = (ushort)this.port;
            c.remoteAddress = this.addr;
        }
    }

    public class NetworkName : Option {
        string sName;
        public NetworkName(string name) {
            this.sName = name;
        }
        void Option.Do(TcpServer s) {
            s.sName = sName;
        }

        void Option.Do(TcpClient c) {
            c.sName = this.sName;
        }
    }
}
