using System;
using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Listener {
    [PublicAPI]
    public sealed class GelfListenerSettings {
        public bool? AllowChunking { get; set; }

        public int? MaxChunkSize { get; set; }

        public TimeSpan? ChunkTimeout { get; set; }

        public int? MaxMessageSize { get; set; }

        public int? MaxUncompressedSize { get; set; }

        public int? NumWorkerThreads { get; set; }

        public int? MaxIncomingQueueSize { get; set; }
    }
}