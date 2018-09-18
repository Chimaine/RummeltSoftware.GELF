using System;
using System.Threading;
using JetBrains.Annotations;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf.Listener {
    /// <summary>
    /// Base class for GELF listener implementations, featuring a parse queue, multiple worker threads and chunking support.
    /// </summary>
    public abstract class ChunkedGelfListenerBase : GelfListenerBase {
        private readonly ChunkBuffer _chunkBuffer;
        private readonly Timer _cleanupTimer;

        // ========================================


        internal ChunkedGelfListenerBase([CanBeNull] GelfListenerSettings settings)
            : base(settings) {
            _chunkBuffer = new ChunkBuffer();
            _cleanupTimer = new Timer(OnCleanupTimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }


        // ========================================

        public override bool SupportsChunking => true;

        // ========================================


        private void OnCleanupTimerCallback(object state) {
            var dropped = _chunkBuffer.ExpireAndCleanup(ChunkTimeout);
            if (dropped > 0) {
                NotifyMessageDropped($"Dropped {dropped} incomplete chunks due to expiration");
            }
        }


        internal override GelfMessageDecoder GetDecoder() {
            return new GelfChunkedMessageDecoder(_chunkBuffer);
        }


        public override void Dispose() {
            _cleanupTimer.Dispose();
            base.Dispose();
            _chunkBuffer.Clear();
        }
    }
}