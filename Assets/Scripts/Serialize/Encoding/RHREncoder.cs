﻿using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace R1Engine {
    /// <summary>
    /// Compresses/decompresses data with the RNC2 algorithm
    /// </summary>
    public class RHREncoder : IStreamEncoder
    {

        public void DecompressBlock_Copy(byte[] compressed, byte[] decompressed, ref int inPos, ref int outPos, ref int toDecompress) {
            Array.Copy(compressed, inPos, decompressed, outPos, toDecompress);
            inPos += toDecompress;
            outPos += toDecompress;
            toDecompress = 0;
        }

        public void DecompressBlock_RLE(byte[] compressed, byte[] decompressed, ref int inPos, ref int outPos, ref int toDecompress) {
            while (toDecompress > 0) {
                byte compressedCount = compressed[inPos++];
                byte uncompressedCount = compressed[inPos++];
                byte repeatByte = compressed[inPos++];
                for (int i = 0; i < compressedCount; i++) {
                    decompressed[outPos + i] = repeatByte;
                }
                for (int i = 0; i < uncompressedCount; i++) {
                    decompressed[outPos + i] = compressed[inPos++];
                }
                toDecompress -= compressedCount + uncompressedCount;
            }
        }

        public void DecompressBlock_Shorts(byte[] compressed, byte[] decompressed, ref int inPos, ref int outPos, ref int toDecompress) {
            using (Reader reader = new Reader(new MemoryStream(compressed))) {
                reader.BaseStream.Position = inPos;
                using (Writer writer = new Writer(new MemoryStream())) {
                    while (toDecompress > 0) {
                        ushort cmd = reader.ReadUInt16();
                        ushort toWrite = (ushort)BitHelpers.ExtractBits(cmd, 14, 0);
                        bool flag14 = BitHelpers.ExtractBits(cmd, 1, 14) == 1;
                        bool flag15 = BitHelpers.ExtractBits(cmd, 1, 15) == 1;
                        if (!flag15) {
                            if (flag14) {
                                writer.Write(toWrite);
                                toDecompress -= 2;
                            }
                            writer.Write(toWrite);
                            toDecompress -= 2;
                        } else {
                            if (!flag14) {
                                byte count = reader.ReadByte();
                                reader.ReadByte(); // Maybe this shouldn't be done?
                                for (int i = 0; i < count; i++) {
                                    writer.Write(toWrite);
                                }
                                toDecompress -= 2*count;
                            } else {
                                writer.Write(toWrite);
                                writer.Write(toWrite);
                                writer.Write(toWrite);
                                toDecompress -= 6;
                            }
                        }
                    }
                    writer.BaseStream.Position = 0;
                    byte[] decompressed2 = (writer.BaseStream as MemoryStream).ToArray();
                    Array.Copy(decompressed2, 0, decompressed, outPos, decompressed2.Length);
                    outPos += decompressed2.Length;
                }
                inPos = (int)reader.BaseStream.Position;
            }
        }

        public void DecompressBlock_Bits(byte[] compressed, byte[] decompressed, ref int inPos, ref int outPos, ref int toDecompress) {
            uint commandOffset = 0;
            int curCommandOffset = 0;
            uint curCommand = 0;
            uint startOffset = (uint)inPos + 4;
            uint curOffset = 0;
            int curCommandBits = 8;
            using (Reader reader = new Reader(new MemoryStream(compressed))) {
                reader.BaseStream.Position = inPos;
                commandOffset = reader.ReadUInt32();
                inPos += 4;
            }
            if (toDecompress > 0) {
                curCommand = compressed[startOffset + commandOffset + curCommandOffset];
            }
            while (toDecompress > 0) {
                if ((curCommand & 0x80) != 0) {
                    curOffset += 2;
                }
                byte bVar1 = compressed[startOffset + curOffset + 1];
                if (bVar1 == 0) {
                    decompressed[outPos++] = compressed[startOffset + curOffset];
                    toDecompress--;
                } else {
                    curOffset += (uint)bVar1 * 2;
                }
                curCommand = curCommand << 1;
                curCommandBits--;
                if (curCommandBits == 0) {
                    curCommandBits = 8;
                    curCommandOffset++;
                    if (toDecompress > 0) {
                        curCommand = compressed[startOffset + commandOffset + curCommandOffset];
                    }
                }
            }
            inPos = compressed.Length;
        }

        public void DecompressBlock_Window(byte[] compressed, byte[] decompressed, ref int inPos, ref int outPos, ref int toDecompress) {
            while (toDecompress > 0) {
                byte cmd = compressed[inPos++];
                if (BitHelpers.ExtractBits(cmd, 1, 7) == 0) {
                    if (cmd == 0 || 8 < cmd) {
                        //UnityEngine.Debug.Log($"1: td{toDecompress}: ip{inPos}, op{outPos}, cmd{cmd}");
                        decompressed[outPos++] = cmd;
                        toDecompress--;
                    } else {
                        //UnityEngine.Debug.Log($"2: td{toDecompress}: ip{inPos}, op{outPos}, cmd{cmd}");
                        for (int i = 0; i < cmd; i++) {
                            decompressed[outPos++] = compressed[inPos++];
                        }
                        toDecompress -= cmd;
                    }
                } else {
                    if (cmd < 0xc0) {
                        int bits = (cmd << 8) | compressed[inPos++];
                        int count = BitHelpers.ExtractBits(bits, 4, 0) + 3;
                        int lookBack = BitHelpers.ExtractBits(bits, 10, 4) + 1;
                        //UnityEngine.Debug.Log($"window lookback: td{toDecompress}: ip{inPos}, op{outPos}, lb{lookBack}, c{count}");
                        for (int i = 0; i < count; i++) {
                            decompressed[outPos] = decompressed[outPos-lookBack];
                            outPos++;
                        }
                        toDecompress-= count;
                    } else {
                        //UnityEngine.Debug.Log($"3: td{toDecompress}: ip{inPos}, op{outPos}, cmd{cmd}");
                        decompressed[outPos++] = 0x20;
                        decompressed[outPos++] = (byte)((cmd & 0x3f) + 0x41);
                        toDecompress -= 2;
                    }
                }
            }
        }

        public void DecompressBlock_Buffer(byte[] compressed, byte[] decompressed, ref int inPos, ref int outPos, ref int toDecompress) {
            while (toDecompress > 0) {
                int blockSz = Math.Min(0x800, toDecompress);
                // Fill temp buffer
                ushort[] buffer = new ushort[0x100];
                for (int i = 0; i < buffer.Length; i++) {
                    buffer[i] = (ushort)i;
                }
                byte fillBufferCount = compressed[inPos++];
                for (int i = 0; i < fillBufferCount; i++) {
                    byte location = compressed[inPos++];
                    byte msb = compressed[inPos++];
                    byte lsb = compressed[inPos++];
                    buffer[location] = (ushort)((msb << 8) | lsb);
                }
                int curSz = 0;
                int puVar6 = 0;
                while (curSz != blockSz) {
                    uint unk = compressed[inPos++];
                    while (true) {
                        while (buffer[unk] != unk) {
                            ushort buf = buffer[unk];
                            unk = (uint)BitHelpers.ExtractBits(buf, 8, 0);
                            puVar6 -= 1;
                            buffer[puVar6 * 2] = (ushort)BitHelpers.ExtractBits(buf, 8, 8);
                            buffer[puVar6 * 2 + 1] = 0;
                        }
                        decompressed[outPos++] = (byte)unk;
                        curSz++;
                        if(puVar6 == 0) break;
                        unk = (uint)(buffer[puVar6 * 2] | (buffer[puVar6 * 2 + 1] << 16));
                        puVar6 += 1;
                    }
                }
                toDecompress -= blockSz;
            }
        }

        public void DecodeBlock_Sum_Byte(byte[] coded, int pos, int length) {
            uint sum = 0;
            for (int i = 0; i < length; i++) {
                sum = (sum + coded[pos + i]) % 0x100;
                coded[pos + i] = (byte)sum;
            }
        }
        public void DecodeBlock_Sum_Short(byte[] coded, int pos, int length) {
            uint sum = 0;
            for (int i = 0; i < length / 2; i++) {
                sum = (sum + (uint)(coded[pos + i*2] | (coded[pos + i*2 + 1] << 8))) % 0x10000;
                coded[pos + i*2] = (byte)(sum & 0xFF);
                coded[pos + i*2 + 1] = (byte)(sum >> 8);
            }
        }
        public void DecodeBlock_Copy(byte[] coded, int pos, int length) {
            // Do nothing
        }
        public void DecodeBlock_Buffer(byte[] coded, int pos, int length) {
            // Fill temp buffer
            byte[] buffer = new byte[0x100];
            for (int i = 0; i < buffer.Length; i++) {
                buffer[i] = (byte)i;
            }
            int curByte = 0;
            for (int i = 0; i < length; i++) {
                byte inByte = coded[pos + i];
                int index = inByte + curByte;
                byte bufByte = buffer[index];
                coded[pos + i] = bufByte;
                if (inByte != 0) {
                    curByte = curByte > 0 ? curByte - 1 : 0xFF;
                    buffer[index] = buffer[curByte];
                    buffer[curByte] = bufByte;
                }
            }
        }


        public byte[] ReadBlock(Reader reader, int decompressedBlockSize) {
            reader.Align(4);
            ushort head = reader.ReadUInt16();
            byte unk0 = reader.ReadByte();
            byte unk1 = reader.ReadByte();
            int compressedSize = reader.ReadInt32();

            int byte2UpperNibble = BitHelpers.ExtractBits(head, 4, 12);
            int byte2LowerNibble = BitHelpers.ExtractBits(head, 4, 8);
            uint local_2c = 0;
            if (byte2UpperNibble > 4) {
                throw new NotImplementedException();
            }
            if (byte2UpperNibble == 5) {
                local_2c = reader.ReadUInt32();
                compressedSize -= 4;
            }
            byte[] compressed = reader.ReadBytes(compressedSize);
            int inPos = 0;
            int outPos = 0;
            int toDecompress = decompressedBlockSize;

            byte[] decompressed = new byte[decompressedBlockSize];

            byte cmd = (byte)BitHelpers.ExtractBits(head,7,0);
            try {
                switch (cmd) {
                    case 0:
                        // Uncompressed
                        DecompressBlock_Copy(compressed, decompressed, ref inPos, ref outPos, ref toDecompress);
                        break;
                    case 1:
                        // RLE
                        DecompressBlock_RLE(compressed, decompressed, ref inPos, ref outPos, ref toDecompress);
                        break;
                    case 2:
                        // Shorts
                        DecompressBlock_Shorts(compressed, decompressed, ref inPos, ref outPos, ref toDecompress);
                        break;
                    case 3:
                        // Bits
                        DecompressBlock_Bits(compressed, decompressed, ref inPos, ref outPos, ref toDecompress);
                        break;
                    case 4:
                        // Window
                        DecompressBlock_Window(compressed, decompressed, ref inPos, ref outPos, ref toDecompress);
                        break;
                    case 5:
                        // Block
                        DecompressBlock_Buffer(compressed, decompressed, ref inPos, ref outPos, ref toDecompress);
                        break;
                }
                switch (byte2LowerNibble) {
                    case 1:
                        DecodeBlock_Sum_Byte(decompressed, 0, decompressed.Length);
                        break;
                    case 2:
                        DecodeBlock_Sum_Short(decompressed, 0, decompressed.Length);
                        break;
                    case 3:
                        DecodeBlock_Copy(decompressed, 0, decompressed.Length);
                        break;
                    case 4:
                        DecodeBlock_Buffer(decompressed, 0, decompressed.Length);
                        break;
                }
                switch (byte2UpperNibble) {
                    case 1:
                        DecodeBlock_Sum_Byte(decompressed, 0, decompressed.Length);
                        break;
                    case 2:
                        DecodeBlock_Sum_Short(decompressed, 0, decompressed.Length);
                        break;
                    case 3:
                        DecodeBlock_Copy(decompressed, 0, decompressed.Length);
                        break;
                    case 4:
                        DecodeBlock_Buffer(decompressed, 0, decompressed.Length);
                        break;
                    case 5:
                    case 6:
                    case 7:
                        throw new NotImplementedException();
                        //break;
                }
            } catch (Exception) {
                //Util.ByteArrayToFile(LevelEditorData.MainContext.BasePath + "crashedBlock.bin", decompressed);
                throw;
            }

            return decompressed;
        }

        /// <summary>
        /// Decodes the data and returns it in a stream
        /// </summary>
        /// <param name="s">The encoded stream</param>
        /// <returns>The stream with the decoded data</returns>
        public Stream DecodeStream(Stream s) {
            Reader reader = new Reader(s, isLittleEndian: true);
            uint totalSize = reader.ReadUInt32();
            byte[] decompressed = new byte[totalSize];
            ushort unk = reader.ReadUInt16();
            ushort blockSize = reader.ReadUInt16();
            uint bytesRead = 0;
            while (bytesRead < totalSize) {
                int toRead = (int)Math.Min(blockSize, totalSize - bytesRead);
                byte[] decompressedBlock = ReadBlock(reader, toRead);
                Array.Copy(decompressedBlock, 0, decompressed, bytesRead, decompressedBlock.Length);
                bytesRead += (uint)decompressedBlock.Length;
            }
            
            var decompressedStream = new MemoryStream(decompressed);

            // Set position back to 0
            decompressedStream.Position = 0;

            // Return the compressed data stream
            return decompressedStream;
        }

        public Stream EncodeStream(Stream s) {
            throw new NotImplementedException();
        }
    }
}