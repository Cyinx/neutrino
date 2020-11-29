using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace neutrino {

    public static class NetworkManager {
        static NetworkManager() {

        }
        public static NetworkComponent ListenTcp(NetworkMgr networkMgr, params Option[] options) {
            TcpServer tcpServer = new TcpServer(networkMgr, options);
            networkMap[tcpServer.Name()] = tcpServer;
            tcpServer.Start();
            return tcpServer;
        }
        public static NetworkComponent ConnectTcp(NetworkMgr networkMgr, params Option[] options) {
            TcpClient tcpClient = new TcpClient(networkMgr, options);
            networkMap[tcpClient.Name()] = tcpClient;
            tcpClient.Start();
            return tcpClient;
        }

        static Dictionary<string, NetworkComponent> networkMap = new Dictionary<string, NetworkComponent>();
    }

}
