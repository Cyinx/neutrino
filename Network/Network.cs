using System;


namespace neutrino {
    enum NetworkCloseState {
        READ_CLOSE,
        WRITE_CLOSE,
        USER_CLOSE,
    }

    public interface SessionHandler {
        void ServeHandler(Agent agent, EventID msgID, Span<byte> memory);
        void ServeRpc(Agent agent, EventID msgID, Span<byte> memory);
    }

    public interface NetworkConn : Agent {
        void Start();
        void Close();
        void WriteRawData(byte msgType, EventID msgID, byte[] b);
    }

    public interface SessionMgr {
        void OnLinkerConneted(NetworkConn network);
        void OnLinkerClosed(NetworkConn network);

        void OnLinkerError(NetworkConn network, Exception ex);
    }

    interface NetworkTransport : NetworkConn {
        void SendMsgPacket(NetworkStream wStream);
        void OnSendComplete();
        void ReadMsgPacket();
        void PingPong();

        SessionMgr GetSessionMgr();
    }

    public interface NetworkMgr : Agent, SessionMgr, SessionHandler {

    }
}
