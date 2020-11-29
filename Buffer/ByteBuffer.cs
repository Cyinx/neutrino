using System;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    public class ByteBuffer {
        protected byte[] buffer;
        private int writePos = 0;
        private int readPos = 0;
        protected int defaultSize = 1024;
        private int memAlignSize = 1024;
        private int needSize = 0;//用于收集写入一个结构时的尺寸
        private bool isGettingSize = false;

        public ByteBuffer()
            : this(-1) {
        }
        public ByteBuffer(int initSize) {
            if (initSize == -1)
                initSize = defaultSize;
            buffer = new byte[initSize];
        }

        public ByteBuffer(byte[] bufferIn) {
            buffer = bufferIn;
            readPos = 0;
            writePos = bufferIn.Length;
        }

        public ByteBuffer(byte[] bufferIn, int offset, int dataSize) {
            buffer = bufferIn;
            readPos = offset;
            writePos = offset + dataSize;
        }

        public void Clone(ByteBuffer rhs) {
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
        public unsafe void WriteByte(byte val) {
            byte* valPtr = &val;
            WriteUnsafeBuff(valPtr, 0, sizeof(byte));
        }

        public unsafe void WriteShort(short val) {
            short* valPtr = &val;
            WriteUnsafeBuff((byte*)valPtr, 0, sizeof(short));
        }

        public unsafe void WriteInt(int val) {
            int* valPtr = &val;
            WriteUnsafeBuff((byte*)valPtr, 0, sizeof(int));
        }

        public unsafe void WriteInt64(Int64 val) {
            Int64* valPtr = &val;
            WriteUnsafeBuff((byte*)valPtr, 0, sizeof(Int64));
        }

        public unsafe void WriteUInt64(UInt64 val) {
            UInt64* valPtr = &val;
            WriteUnsafeBuff((byte*)valPtr, 0, sizeof(UInt64));
        }

        public unsafe void WriteDouble(double val) {
            double* valPtr = &val;
            WriteUnsafeBuff((byte*)valPtr, 0, sizeof(double));
        }

        public unsafe void WriteString(string val) {
            short byteLen = (short)(val.Length * sizeof(char));
            WriteShort(byteLen);
            fixed (char* str = val)   //var is string
            {
                WriteUnsafeBuff((byte*)str, 0, byteLen);
            }
        }

        public unsafe void WriteStringUtf8(string val) {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(val);
            WriteShort((short)buffer.Length);
            Write(buffer);
        }

        public unsafe void WriteString(char[] val) {
            short byteLen = (short)(val.Length * sizeof(char));
            WriteShort(byteLen);
            fixed (char* str = val)   //var is string
            {
                WriteUnsafeBuff((byte*)str, 0, byteLen);
            }
        }

        public unsafe void WriteStringUtf8(char[] val) {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(val);
            WriteShort((short)buffer.Length);
            Write(buffer);
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

        public unsafe short ReadShort() {
            if (readPos >= buffer.Length) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            fixed (byte* resultPtr = &buffer[readPos]) {
                short result = *((short*)resultPtr);
                readPos += sizeof(short);
                return result;
            }
        }

        public unsafe int ReadInt() {
            if (readPos >= buffer.Length) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            fixed (byte* resultPtr = &buffer[readPos]) {
                int result = *((int*)resultPtr);
                readPos += sizeof(int);
                return result;
            }
        }

        public unsafe Int64 ReadInt64() {
            if (readPos >= buffer.Length) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            fixed (byte* resultPtr = &buffer[readPos]) {
                Int64 result = *((Int64*)resultPtr);
                readPos += sizeof(Int64);
                return result;
            }
        }

        public unsafe UInt64 ReadUInt64() {
            if (readPos >= buffer.Length) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            fixed (byte* resultPtr = &buffer[readPos]) {
                UInt64 result = *((UInt64*)resultPtr);
                readPos += sizeof(UInt64);
                return result;
            }
        }

        public unsafe double ReadDouble() {
            if (readPos >= buffer.Length) {
                throw new Exception("Buffer Error: ReadByte: readPos great than buffer.Length"
                    + (new System.Diagnostics.StackTrace()).ToString());
            }

            double result = 0;
            byte* resultPtr = (byte*)&result;
            for (int i = 0; i < sizeof(double); ++i) {
                resultPtr[i] = buffer[readPos + i];
            }
            readPos += sizeof(double);
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

        public void Reset(ByteBuffer rhs) {
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

        public void Write(byte[] buf) {
            Write(buf, 0, buf.Length);
        }

        public byte[] GetBuffer() {
            return buffer;
        }

        public byte[] ToByteArray() {
            return ToByteArray(0);
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
                Buffer.MemoryCopy(dataPtr, beginPtr, dataSize, Capacity);
            }
            readPos = 0;
            writePos = dataSize;
        }
    }
}
