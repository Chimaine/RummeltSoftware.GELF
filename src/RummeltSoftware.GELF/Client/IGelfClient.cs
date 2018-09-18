using System;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Client {
    [PublicAPI]
    public interface IGelfClient : IDisposable {
        bool SupportsChunking { get; }

        bool SupportsCompression { get; }

        bool AllowChunking { get; set; }

        int MaxChunkSize { get; }

        int MaxCompressedSize { get; }

        int MaxUncompressedSize { get; }

        CompressionMethod CompressionMethod { get; }

        int CompressionThreshold { get; }

        void Send(GelfMessage message);
    }
}