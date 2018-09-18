using System;
using System.IO;
using System.Runtime.CompilerServices;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Listener.Internal {
    /// <inheritdoc />
    internal sealed class GelfChunkedMessageDecoder : GelfMessageDecoder {
        private readonly ChunkBuffer _chunkBuffer;

        // ========================================


        public GelfChunkedMessageDecoder(ChunkBuffer chunkBuffer) {
            _chunkBuffer = chunkBuffer;
        }


        // ========================================


        public override GelfMessageReceivedEventArgs Decode(RawGelfMessage rawMessage) {
            return IsChunkedMessage(rawMessage.Bytes)
                ? DecodeChunk(rawMessage)
                : base.Decode(rawMessage);
        }


        // ========================================


        private GelfMessageReceivedEventArgs DecodeChunk(RawGelfMessage chunk) {
            RawGelfMessage[] chunks;
            try {
                if (!_chunkBuffer.TryCompleteMessage(chunk, out chunks))
                    return null;
            }
            catch (Exception) {
                chunk.Dispose();
                throw;
            }

            try {
                var firstChunk = chunks[0];
                var compressionMethod = GetCompressionMethod(firstChunk.Bytes, 0xC);

                if (compressionMethod == CompressionMethod.None)
                    throw new GelfMessageFormatException("Uncompressed chunked messages are not supported");

                var receivedAt = firstChunk.ReceivedAt;
                for (var i = 1; i < chunks.Length; i++) {
                    var chunkReceivedAt = chunks[i].ReceivedAt;
                    if (chunkReceivedAt < receivedAt) {
                        receivedAt = chunkReceivedAt;
                    }
                }

                using (var stream = MemoryStreamPool.GetStream("GelfListener.Decode")) {
                    Decompress(chunks, stream, compressionMethod);

                    var message = Decode(stream);
                    return new GelfMessageReceivedEventArgs(message, firstChunk.ReceivedFrom, receivedAt);
                }
            }
            finally {
                foreach (var c in chunks) {
                    c.Dispose();
                }
            }
        }


        // ========================================


        private static void Decompress(RawGelfMessage[] chunks, Stream output, CompressionMethod method) {
            using (var deflater = CreateDecompressingStream(method, output)) {
                var offset = 0;
                foreach (var chunk in chunks) {
                    deflater.Write(chunk.Bytes, offset, chunk.ByteCount);
                    offset += chunk.ByteCount;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsChunkedMessage(byte[] bytes) {
            return bytes[0] == 0x1E && bytes[1] == 0x0F;
        }
    }
}