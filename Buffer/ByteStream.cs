using System;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    public class ByteStream {
        protected byte[] buffer;
        protected int writePos = 0;
        protected int readPos = 0;
        const int defaultSize = 1024;
        const int memAlignSize = 1024;
        protected int needSize = 0;
        protected bool isGettingSize = false;
        public ByteStream()
            : this(-1) {
        }
        public ByteStream(int initSize) {
            if (initSize == -1)
                initSize = defaultSize;
            buffer = new byte[initSize];
        }

        public ByteStream(byte[] bufferIn) {
            buffer = bufferIn;
            readPos = 0;
            writePos = bufferIn.Length;
        }

        public ByteStream(byte[] bufferIn, int offset, int dataSize) {
            buffer = bufferIn;
            readPos = offset;
            writePos = offset + dataSize;
        }

        public void Clone(ByteStream rhs) {
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

        public void Reset(ByteStream rhs) {
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

        public byte[] GetBuffer() {
            return buffer;
        }

        public byte[] ToByteArray() {
            return ToByteArray(0);
        }

        public unsafe Span<byte> SpanBytes(int readCount) {
            if (readCount == 0)
                readCount = Size;
            if (readCount > Size)
                readCount = Size;

            Span<byte> slice = null;
            fixed(byte* beginPtr = &buffer[readPos]) {
                slice = new Span<byte>(beginPtr, readCount);
            }
            readPos += readCount;
            return slice;
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
