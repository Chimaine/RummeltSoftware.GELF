using System;
using System.IO;
using System.IO.Compression;
using RummeltSoftware.Gelf.Client.Internal;
using RummeltSoftware.Gelf.Internal;

namespace RummeltSoftware.Gelf.Client {
    /// <summary>
    /// Base class for GELF client implementations, featuring chunking and compression support.
    /// </summary>
    public abstract class GelfClientBase : IGelfClient {
        private readonly GelfMessageEncoder _encoder;

        // ========================================

        public abstract bool SupportsChunking { get; }

        public abstract bool SupportsCompression { get; }

        public bool AllowChunking { get; set; }

        public int MaxChunkSize { get; }

        public int MaxCompressedSize { get; }

        public int MaxUncompressedSize { get; }

        public CompressionMethod CompressionMethod { get; }

        public int CompressionThreshold { get; }

        // ========================================


        protected GelfClientBase(GelfClientSettings settings) {
            AllowChunking = settings.AllowChunking ?? true;
            MaxChunkSize = settings.MaxChunkSize ?? 8192;
            MaxCompressedSize = settings.MaxCompressedSize ?? 1048576;
            MaxUncompressedSize = settings.MaxUncompressedSize ?? 8388608;
            CompressionMethod = settings.CompressionMethod ?? CompressionMethod.GZIP;
            CompressionThreshold = settings.CompressionThreshold ?? 1024;

            _encoder = new GelfMessageEncoder();
        }


        // ========================================


        public void Send(GelfMessage message) {
            using (var payloadStream = MemoryStreamPool.GetStream("GelfClient.Encode")) {
                _encoder.Encode(message, payloadStream);
                payloadStream.Position = 0;

                if (payloadStream.Length > MaxUncompressedSize)
                    throw new GelfException($"Message exceeds {nameof(MaxUncompressedSize)} limit ({payloadStream.Length} > {MaxUncompressedSize})");

                if (ShouldCompress(payloadStream)) {
                    CompressAndSend(payloadStream);
                }
                else {
                    Send(payloadStream);
                }
            }
        }


        // ========================================

        protected abstract void Send(MemoryStream stream);

        public abstract void Dispose();

        // ========================================


        private bool ShouldCompress(Stream stream) {
            if (!SupportsCompression || (CompressionMethod == CompressionMethod.None))
                return false;

            if (SupportsChunking)
                return stream.Length >= Math.Min(CompressionThreshold, MaxChunkSize + 1);

            return stream.Length >= CompressionThreshold;
        }


        private void CompressAndSend(MemoryStream payloadStream) {
            using (var compressedStream = MemoryStreamPool.GetStream("GelfClient.Compress")) {
                Compress(payloadStream, compressedStream, CompressionMethod);
                compressedStream.Position = 0;

                if (compressedStream.Length > MaxCompressedSize)
                    throw new GelfException($"Message exceeds {nameof(MaxCompressedSize)} limit ({compressedStream.Length} > {MaxCompressedSize})");

                Send(compressedStream);
            }
        }


        private static void Compress(MemoryStream input, Stream output, CompressionMethod method) {
            using (var deflater = CreateCompressingStream(method, output)) {
                input.WriteTo(deflater);
            }
        }


        private static Stream CreateCompressingStream(CompressionMethod method, Stream output) {
            switch (method) {
                case CompressionMethod.GZIP:
                    return new GZipStream(output, CompressionMode.Compress, true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown compression method");
            }
        }
    }
}