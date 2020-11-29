using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace neutrino {

    // --------------------------------------------------------------------------------------------------------
    // |                                header              |                body                          |
    // | type byte | body_length uint32 | packet_flag uint8 | msg_id uint32| msg_data []byte               |
    // --------------------------------------------------------------------------------------------------------
    public class NetworkStream {
        protected byte[] buffer;
        private int writePos = 0;
        private int readPos = 0;
        protected int defaultSize = 1024;
        private int memAlignSize = 1024;
        private int needSize = 0;
        private bool isGettingSize = false;
        public NetworkStream()
            : this(-1) {
        }
        public NetworkStream(int initSize) {
            if (initSize == -1)
                initSize = defaultSize;
            buffer = new byte[initSize];
        }

        public NetworkStream(byte[] bufferIn) {
            buffer = bufferIn;
            readPos = 0;
            writePos = bufferIn.Length;
        }

        public NetworkStream(byte[] bufferIn, int offset, int dataSize) {
            buffer = bufferIn;
            readPos = offset;
            writePos = offset + dataSize;
        }

        public void Clone(NetworkStream rhs) {
            Reset();

            int dataSize = rhs.Size;
            readPos = 0;
            writePos = 0;
            Reserve(dataSize);

            Buffer.BlockCopy(rhs.buffer, rhs.readPos, buffer, 0, dataSize);
            writePos = dataSize;
        }

        public int ReadPos { get { return readPos; } }
        public int WritePos { get { return writePos; } }
        public int Size { get { return WritePos - ReadPos; } }
        public int Capacity { get { return buffer.Length; } }
        public int Space { get { return Capacity - writePos; } }
        public int PreSpace { get { return readPos; } }

        public int Read(byte[] buf, int outOffset, int readSize) {
            int resultPos = readPos + readSize;
            if (resultPos > writePos) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            // 允许移动读取指针
            if (buf != null)
                Buffer.BlockCopy(buffer, readPos, buf, outOffset, readSize);
            readPos = resultPos;
            return readSize;
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

        public void Reserve(int needSize) {
            if (Space > needSize)
                return;
            if (PreSpace > needSize) {
                MoveDataToBegin();
                return;
            }

            if (buffer.Length < (writePos + needSize)) {
                try {
                    int oldSize = Size;
                    int needCapacity = oldSize + needSize;
                    int newCapacity = Capacity;
                    if (newCapacity == 0)
                        newCapacity = defaultSize;

                    while (newCapacity < needCapacity) {
                        // 大于64K时,尺寸不再按乘以2计算
                        if (newCapacity >= (64 * 1024))
                            newCapacity += 4 * memAlignSize;
                        else
                            newCapacity = newCapacity << 1;
                    }

                    byte[] newbuf = new byte[newCapacity];
                    Buffer.BlockCopy(buffer, readPos, newbuf, 0, oldSize);
                    buffer = newbuf;
                    readPos = 0;
                    writePos = oldSize;
                } catch (Exception ex) {
                    throw ex;
                }
            } else {
                if (readPos > (4 * 1024) && readPos > (buffer.Length / 3))
                    MoveDataToBegin();
            }
        }

        public void Reset(byte[] newRefBuffer, int dataOffset, int dataSize) {
            try {
                buffer = newRefBuffer;
                writePos = dataOffset + dataSize;
                readPos = dataOffset;
            } catch (Exception ex) {
                throw ex;
            }
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

        public void Reset() {
            writePos = 0;
            readPos = 0;
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

        public byte[] GetBuffer() {
            return buffer;
        }

        public byte[] ToByteArray() {
            return ToByteArray(0);
        }

        public byte[] SpanBytes(int readCount) {
            if (readCount == 0)
                readCount = Size;
            if (readCount > Size)
                readCount = Size;
            var b = buffer[readPos..(readPos + readCount)];
            readPos += readCount;
            return b;
        }
        public byte[] ToByteArray(int readCount) {
            if (readCount == 0)
                readCount = Size;
            if (readCount == 0)
                return new byte[0];
            byte[] ret = new byte[readCount];
            Buffer.BlockCopy(buffer, readPos, ret, 0, readCount);
            return ret;
        }

        public void GetSizeBegin() {
            isGettingSize = true;
            needSize = 0;
        }

        public int GetSizeEnd() {
            isGettingSize = false;
            return needSize;
        }

        public unsafe void MoveDataToBegin() {
            //Unsafe模式
            int dataSize = Size;
            fixed (byte* beginPtr = &buffer[0], dataPtr = &buffer[readPos]) {
                Buffer.MemoryCopy(dataPtr, beginPtr, dataSize, Size);
            }
            readPos = 0;
            writePos = dataSize;
        }
    }
}
