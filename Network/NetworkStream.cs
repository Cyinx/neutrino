using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace neutrino {

    // --------------------------------------------------------------------------------------------------------
    // |                                header              |                body                          |
    // | type byte | body_length uint32 | packet_flag uint8 | msg_id uint32| msg_data []byte               |
    // --------------------------------------------------------------------------------------------------------
    public class NetworkStream : ByteStream {
        public NetworkStream()
            : base(-1) {
        }
        public NetworkStream(int initSize) : base(initSize) {

        }

        public NetworkStream(byte[] bufferIn) : base(bufferIn) {

        }

        public NetworkStream(byte[] bufferIn, int offset, int dataSize) : base(bufferIn, offset, dataSize) {

        }

        // 摸拟流的函数
        public void WriteByte(byte val) {
            Reserve(sizeof(byte));
            buffer[writePos] = val;
            writePos += sizeof(byte);
        }

        public void WriteInt16(short val) {
            Reserve(sizeof(short));
            buffer[writePos + 0] = (byte)(val >> 8);
            buffer[writePos + 1] = (byte)(val);
            writePos += sizeof(short);
        }

        public void WriteUInt16(ushort val) {
            Reserve(sizeof(ushort));
            buffer[writePos + 0] = (byte)(val >> 8);
            buffer[writePos + 1] = (byte)(val);
            writePos += sizeof(ushort);
        }

        public void WriteUInt32(UInt32 val) {
            Reserve(sizeof(UInt32));
            buffer[writePos + 0] = (byte)(val >> 24);
            buffer[writePos + 1] = (byte)(val >> 16);
            buffer[writePos + 2] = (byte)(val >> 8);
            buffer[writePos + 3] = (byte)(val);
            writePos += sizeof(UInt32);
        }

        public void WriteInt32(Int32 val) {
            Reserve(sizeof(Int32));
            buffer[writePos + 0] = (byte)(val >> 24);
            buffer[writePos + 1] = (byte)(val >> 16);
            buffer[writePos + 2] = (byte)(val >> 8);
            buffer[writePos + 3] = (byte)(val);
            writePos += sizeof(Int32);
        }

        public byte ReadByte() {
            if (readPos >= buffer.Length) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }
            byte result = buffer[readPos];
            readPos += 1;
            return result;
        }
        public short ReadInt16() {
            if (readPos >= buffer.Length || Size < sizeof(short)) {
                throw new Exception("Buffer Error: ReadInt16: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            short result = (short)(buffer[readPos + 1] | (((short)buffer[readPos]) << 8));
            readPos += sizeof(short);
            return result;
        }

        public ushort ReadUInt16() {
            if (readPos >= buffer.Length || Size < sizeof(ushort)) {
                throw new Exception("Buffer Error: ReadUInt16: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            ushort result = (UInt16)(buffer[readPos + 1] | (((UInt16)buffer[readPos]) << 8));
            readPos += sizeof(ushort);
            return result;
        }

        public int ReadInt32() {
            if (readPos >= buffer.Length || Size < sizeof(int)) {
                throw new Exception("Buffer Error: ReadInt32: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            int result = (int)((int)buffer[readPos + 3] | ((int)buffer[readPos + 2]) << 8 | ((int)buffer[readPos + 1]) << 16 | ((int)buffer[readPos]) << 24);
            readPos += sizeof(int);
            return result;
        }

        public uint ReadUInt32() {
            if (readPos >= buffer.Length || Size < sizeof(uint)) {
                throw new Exception("Buffer Error: ReadUInt32: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }
            uint result = (uint)((uint)buffer[readPos + 3] | ((uint)buffer[readPos + 2]) << 8 | ((uint)buffer[readPos + 1]) << 16 | ((uint)buffer[readPos]) << 24);
            readPos += sizeof(uint);
            return result;
        }
        public Int64 ReadInt64() {
            if (readPos >= buffer.Length || Size < sizeof(Int64)) {
                throw new Exception("Buffer Error: ReadInt64: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            Int64 result = (Int64)((Int64)buffer[readPos + 7] | ((Int64)buffer[readPos + 6]) << 8 | ((Int64)buffer[readPos + 5]) << 16 | ((Int64)buffer[readPos + 4]) << 24
                | ((Int64)buffer[readPos + 3]) << 32 | ((Int64)buffer[readPos + 2]) << 40 | ((Int64)buffer[readPos + 1]) << 48 | ((Int64)buffer[readPos]) << 56);

            readPos += sizeof(Int64);
            return result;
        }

        public UInt64 ReadUInt64() {
            if (readPos >= buffer.Length || Size < sizeof(UInt64)) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            UInt64 result = (UInt64)((UInt64)buffer[readPos + 7] | ((UInt64)buffer[readPos + 6]) << 8 | ((UInt64)buffer[readPos + 5]) << 16 | ((UInt64)buffer[readPos + 4]) << 24
                  | ((UInt64)buffer[readPos + 3]) << 32 | ((UInt64)buffer[readPos + 2]) << 40 | ((UInt64)buffer[readPos + 1]) << 48 | ((UInt64)buffer[readPos]) << 56);

            readPos += sizeof(UInt64);
            return result;
        }
        public void Reset(NetworkStream rhs) {
            try {
                buffer = rhs.buffer;
                writePos = rhs.writePos;
                readPos = rhs.readPos;
            } catch (Exception ex) {
                throw ex;
            }
        }
        public void Write(byte[] buf, int srcOff, int writeSize) {
            Reserve(writeSize);
            if (isGettingSize) {
                needSize += writeSize;
                return;
            }

            if (buf != null)
                Buffer.BlockCopy(buf, srcOff, buffer, writePos, writeSize);
            writePos += writeSize;
        }

        public unsafe void WriteUnsafeBuff(byte* buf, int srcOff, int writeSize) {
            Reserve(writeSize);
            if (isGettingSize) {
                needSize += writeSize;
                return;
            }

            for (int i = 0; i < writeSize; ++i) {
                buffer[writePos + i] = buf[srcOff + i];
            }
            writePos += writeSize;
        }

        public void TransferredBytes(int writeSize) {
            writePos += writeSize;
        }

        public void WriteBytes(byte[] buf) {
            Write(buf, 0, buf.Length);
        }
    }
}
