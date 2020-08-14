﻿using System;
using System.IO;

namespace R1Engine
{

    /// <summary>
    /// Compresses/decompresses data using LZSS
    /// </summary>
    public class RRRGBA_LZSSEncoder : IStreamEncoder
    {
        uint length;
        public RRRGBA_LZSSEncoder(uint length) {
            this.length = length - 4;
        }
        /// <summary>
        /// Decodes the data and returns it in a stream
        /// </summary>
        /// <param name="s">The encoded stream</param>
        /// <returns>The stream with the decoded data</returns>
        public Stream DecodeStream(Stream s) {
            var decompressedStream = new MemoryStream();

            Reader reader = new Reader(s, isLittleEndian: true); // No using, because we don't want to close the stream
            uint magic = reader.ReadUInt32();

            if (magic != 0x01234567)
                throw new InvalidDataException("The data is not LZSS compressed!");
            byte[] bytes = reader.ReadBytes((int)length);

            using (MemoryStream ms = new MemoryStream(bytes)) {
                LzssAlgorithm.Lzss.Decode(ms, decompressedStream);
            }

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