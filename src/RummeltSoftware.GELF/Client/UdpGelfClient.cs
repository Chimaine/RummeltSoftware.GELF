using System;
using System.IO;
using System.Net.Sockets;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Client {
    /// <summary>
    /// UDP sockets based GELF client, implementing the official spec for UDP. Supports compression and chunking.
    /// </summary>
    public sealed class UdpGelfClient : GelfClientBase {
        private readonly UdpClient _client;

        private ulong _nextMessageID;

        // ========================================


        public UdpGelfClient(string host, int port, GelfClientSettings settings)
            : base(settings) {
            Host = host;
            Port = port;

            _client = new UdpClient(host, port);
        }


        // ========================================

        public override bool SupportsChunking => true;

        public override bool SupportsCompression => true;

        public string Host { get; }

        public int Port { get; }

        // ========================================


        protected override void Send(MemoryStream stream) {
            if (ShouldChunk(stream)) {
                SendChunked(stream);
            }
            else {
                SendUnchunked(stream);
            }
        }


        // ========================================


        private bool ShouldChunk(Stream stream) {
            return AllowChunking && (stream.Length > MaxChunkSize);
        }


        private void SendUnchunked(MemoryStream stream) {
            var data = stream.GetBuffer();
            var dataLength = (int) stream.Length; // !

            _client.Send(data, dataLength);
        }


        private void SendChunked(Stream stream) {
            var buffer = ByteArrayPool.Rent(MaxChunkSize);
            try {
                var messageID = _nextMessageID++;
                var nChunks = (int) Math.Ceiling(stream.Length / (MaxChunkSize - 12.0));

                // Chunked GELF magic bytes
                buffer[0x0] = 0x1E;
                buffer[0x1] = 0x0F;

                // Message ID
                buffer[0x2] = (byte) messageID;
                buffer[0x3] = (byte) (messageID >> 8);
                buffer[0x4] = (byte) (messageID >> 16);
                buffer[0x5] = (byte) (messageID >> 24);
                buffer[0x6] = (byte) (messageID >> 32);
                buffer[0x7] = (byte) (messageID >> 40);
                buffer[0x8] = (byte) (messageID >> 48);
                buffer[0x9] = (byte) (messageID >> 56);

                // Sequence count
                buffer[0xB] = (byte) nChunks;

                for (var i = 0; i < nChunks; i++) {
                    // Sequence number
                    buffer[0xA] = (byte) i;

                    // Body
                    var chunkSize = stream.Read(buffer, 12, MaxChunkSize - 12) + 12;

                    _client.Send(buffer, chunkSize);
                }
            }
            finally {
                ByteArrayPool.Return(buffer);
            }
        }


        // ========================================


        public override void Dispose() {
            _client.Dispose();
        }
    }
}