using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;

namespace neutrino {
    enum RecvProcessStep {
        NONE,
        HEAD,
        BODY,
        COMPLETE,
    }

    partial class TcpConn : NetworkTransport {

        public const int MSG_DEFAULT_LENGTH = 1024;
        public const int MSG_HEADER_LENGTH = 6;
        public const int MSG_ID_LENGTH = 4;

        public void WriteRawData(byte msgType, EventID msgID, byte[] b) {
            var buf = TransportManager.GetWriteStream();
            buf.Reserve(b.Length + MSG_HEADER_LENGTH + MSG_ID_LENGTH);
            buf.WriteByte(msgType);
            buf.WriteUInt32((UInt32)(b.Length + MSG_ID_LENGTH));
            buf.WriteByte(0);
            buf.WriteUInt32(msgID);
            buf.WriteBytes(b);
            TransportManager.QueueWriteBuffer(this, buf);
        }
        SessionMgr NetworkTransport.GetSessionMgr() {
            return networkMgr;
        }
        void NetworkTransport.PingPong() {

        }
        void NetworkTransport.ReadMsgPacket() {
            recvBuffer.Reserve(MSG_DEFAULT_LENGTH);
            readEventArg.SetBuffer(recvBuffer.GetBuffer(), recvBuffer.WritePos, recvBuffer.Space);
            bool willRaiseEvent = tcpSocket.ReceiveAsync(readEventArg);
            if (!willRaiseEvent) {
                OnSocketRead(readEventArg);
            }
        }
        void NetworkTransport.SendMsgPacket(NetworkStream writeBuffer) {
            if (inSending == false) {
                inSending = true;
                writeEventArg.SetBuffer(writeBuffer.GetBuffer(), 0, writeBuffer.Size);
                writeEventArg.UserToken = writeBuffer;
                var willRaiseEvent = tcpSocket.SendAsync(writeEventArg);
                if (!willRaiseEvent) {
                    OnSocketSend(writeEventArg);
                }
            } else {
                writeQueue.Enqueue(writeBuffer);
            }
        }

        void NetworkTransport.OnSendComplete() {
            inSending = false;
            NetworkStream writeBuffer = null;
            if (writeQueue.TryDequeue(out writeBuffer) == false) {
                return;
            }
            NetworkTransport transport = this;
            transport.SendMsgPacket(writeBuffer);
        }

        private void OnSocketSend(SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                StartClose(NetworkCloseState.WRITE_CLOSE);
                return;
            }

            var wBuffer = (NetworkStream)e.UserToken;
            TransportManager.OnSendComplete(this, wBuffer);
        }
        private void ReseveRecvBuf(UInt32 len) {
            if (recvBuffer.Space <= len) {
                return;
            }
            recvBuffer.Reserve((int)len);
        }

        private void OnSocketRead(SocketAsyncEventArgs e) {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success) {
                StartClose(NetworkCloseState.READ_CLOSE);
                return;
            }

            recvBuffer.TransferredBytes(e.BytesTransferred);
            do {
                switch (recvStep) {
                    case RecvProcessStep.NONE:
                        if (recvBuffer.Space < MSG_HEADER_LENGTH) {
                            recvBuffer.Reserve(MSG_HEADER_LENGTH);
                        }
                        recvStep = RecvProcessStep.HEAD;
                        goto case RecvProcessStep.HEAD;
                    case RecvProcessStep.HEAD:
                        if (recvBuffer.Size < MSG_HEADER_LENGTH) {
                            break;
                        }
                        recvMsgType = recvBuffer.ReadByte();
                        if (recvMsgType == 0x54) { // 'T'
                            recvStep = RecvProcessStep.NONE;
                            break;
                        }
                        recvBodyLenght = recvBuffer.ReadUInt32();
                        recvBuffer.ReadByte(); //read flag
                        recvStep = RecvProcessStep.BODY;
                        goto case RecvProcessStep.BODY;
                    case RecvProcessStep.BODY:
                        if (recvBuffer.Size >= recvBodyLenght)
                            goto case RecvProcessStep.COMPLETE;
                        ReseveRecvBuf(recvBodyLenght);
                        break;
                    case RecvProcessStep.COMPLETE:
                        recvStep = RecvProcessStep.COMPLETE;
                        OnTakeSingleMsg();
                        recvStep = RecvProcessStep.NONE;
                        goto case RecvProcessStep.NONE;
                    default:
                        break;
                }

            } while (false);

            NetworkTransport transport = this;
            transport.ReadMsgPacket();
        }

        private void OnTakeSingleMsg() {
            EventID msgID = recvBuffer.ReadUInt32();
            var b = recvBuffer.SpanBytes((int)recvBodyLenght);
            switch (recvMsgType) {
                case 0x50: // 'P'
                    networkMgr.ServeHandler(this, msgID, b);
                    break;
                case 0x52: // 'R'
                    networkMgr.ServeRpc(this, msgID, b);
                    break;
                default:
                    break;
            }
        }

        //write 
        ConcurrentQueue<NetworkStream> writeQueue = new ConcurrentQueue<NetworkStream>();
        bool inSending = false;

        NetworkStream recvBuffer = new NetworkStream(MSG_DEFAULT_LENGTH);
        RecvProcessStep recvStep = RecvProcessStep.NONE;
        UInt32 recvBodyLenght = 0;
        byte recvMsgType = 0;
    }
}