using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    enum NetworkWorkerEvent : int {
        QUEUE_SEND_MSG,
        CONTINUE_SEND_MSG,
        ADD_NETWORK_CONN,
        REMOVE_NETWORK_CONN
    }
    static class TransportManager {

        static TransportManager() {
            var m = new ExecutorWorker("networkManager");
            m.Init();
            worker = m;
            worker.RegisterEventHandler((int)NetworkWorkerEvent.QUEUE_SEND_MSG, OnQueueSendMsg);
            worker.RegisterEventHandler((int)NetworkWorkerEvent.CONTINUE_SEND_MSG, OnContinueSendMsg);
            worker.RegisterEventHandler((int)NetworkWorkerEvent.ADD_NETWORK_CONN, OnAddNetworkConn);
            worker.RegisterEventHandler((int)NetworkWorkerEvent.REMOVE_NETWORK_CONN, OnRemoveNetworkConn);
        }

        internal static void Start() {

        }

        internal static void Close() {
            worker.Close();
        }

        public static void AddNetworkConn(NetworkTransport conn) {
            worker.Enqueue(conn, (int)NetworkWorkerEvent.ADD_NETWORK_CONN);
        }
        public static void CloseNetworkConn(NetworkTransport conn) {
            worker.Enqueue(conn, (int)NetworkWorkerEvent.REMOVE_NETWORK_CONN);
        }
        public static void QueueWriteBuffer(NetworkTransport conn, NetworkStream writeStream) {
            worker.Enqueue(conn, (int)NetworkWorkerEvent.QUEUE_SEND_MSG, writeStream);
        }

        public static void OnSendComplete(NetworkTransport conn, NetworkStream writeStream) {
            worker.Enqueue(conn, (int)NetworkWorkerEvent.CONTINUE_SEND_MSG, writeStream);
        }

        public static NetworkStream GetWriteStream() {
            NetworkStream newBuffer = null;
            if (freeBuffer.TryDequeue(out newBuffer) == false) {
                newBuffer = new NetworkStream();
            }
            return newBuffer;
        }

        static void OnQueueSendMsg(Context ctx, object arg) {
            NetworkTransport conn = (NetworkTransport)ctx.GetSender();
            NetworkStream wBuffer = (NetworkStream)arg;
            conn.SendMsgPacket(wBuffer);
        }

        static void OnContinueSendMsg(Context ctx, object arg) {
            NetworkTransport conn = (NetworkTransport)ctx.GetSender();
            NetworkStream wBuffer = (NetworkStream)arg;
            wBuffer.Reset();
            freeBuffer.Enqueue(wBuffer);
            conn.OnSendComplete();
        }


        static void OnAddNetworkConn(Context ctx, object arg) {
            NetworkTransport conn = (NetworkTransport)ctx.GetSender();
            connMap[conn.GetID()] = conn;
            SessionMgr sessionMgr = conn.GetSessionMgr();
            sessionMgr.OnLinkerConneted(conn);
            conn.ReadMsgPacket();
            var timerID = worker.AddTimer(5 * 1000, OnNetworkConnPing, conn);
            tickTimer[conn.GetID()] = timerID;
        }

        static void OnNetworkConnPing(object[] args) {
            NetworkTransport conn = (NetworkTransport)args[0];
            conn.PingPong();
            var timerID = worker.AddTimer(5 * 1000, OnNetworkConnPing, conn);
            tickTimer[conn.GetID()] = timerID;
        }

        static void OnRemoveNetworkConn(Context ctx, object arg) {
            NetworkTransport conn = (NetworkTransport)ctx.GetSender();
            connMap.Remove(conn.GetID());
            SessionMgr sessionMgr = conn.GetSessionMgr();
            sessionMgr.OnLinkerClosed(conn);
            UInt64 timerID = 0;
            if (tickTimer.TryGetValue(conn.GetID(), out timerID) == true) {
                worker.RemoveTimer(timerID);
            }
        }

        static ConcurrentQueue<NetworkStream> freeBuffer = new ConcurrentQueue<NetworkStream>();
        static Dictionary<AgentID, NetworkTransport> connMap = new Dictionary<AgentID, NetworkTransport>();
        static Dictionary<AgentID, UInt64> tickTimer = new Dictionary<AgentID, UInt64>();
        static Executor worker = null;
    }
}
