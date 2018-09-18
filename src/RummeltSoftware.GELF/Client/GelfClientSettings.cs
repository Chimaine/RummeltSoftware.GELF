using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Client {
    [PublicAPI]
    public sealed class GelfClientSettings {
        public bool? AllowChunking { get; set; }

        public int? MaxChunkSize { get; set; }

        public int? MaxCompressedSize { get; set; }

        public int? MaxUncompressedSize { get; set; }

        public CompressionMethod? CompressionMethod { get; set; }

        public int? CompressionThreshold { get; set; }
    }
}