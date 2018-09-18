using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RummeltSoftware.Gelf.Listener.Internal {
    internal sealed class ChunkBuffer {
        private readonly Dictionary<ulong, Entry> _entries;
        private readonly ReaderWriterLockSlim _lock;

        // ========================================


        public ChunkBuffer() {
            _entries = new Dictionary<ulong, Entry>(64);
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }


        // ========================================


        /// <summary>
        /// How many incomplete messages this buffer currently holds.
        /// </summary>
        public int Count {
            get {
                _lock.EnterReadLock();
                try {
                    return _entries.Count;
                }
                finally {
                    _lock.ExitReadLock();
                }
            }
        }


        // ========================================


        /// <summary>
        /// Adds a chunk to this buffer. If the message is now complete, all chunks
        /// in this message are returned and it is removed from this buffer.
        /// </summary>
        /// <param name="chunk">The chunk to add.</param>
        /// <param name="completeMessage">
        /// Contains all chunks of a message if the returned value is <c>true</c>, <c>null</c>
        /// otherwise.
        /// </param>
        /// <returns><c>true</c> if the message is now complete, <c>false</c> otherwise.</returns>
        public bool TryCompleteMessage(RawGelfMessage chunk, out RawGelfMessage[] completeMessage) {
            var messageID = ReadMessageID(chunk.Bytes);
            var sequenceNumber = ReadChunkIndex(chunk.Bytes);
            var sequenceCount = ReadChunkCount(chunk.Bytes);

            if (sequenceCount > 128)
                throw new TooManyChunksException($"Exceeded maximum number of chunks: {sequenceCount}");

            var entry = GetEntry(messageID, sequenceCount);
            entry.AddChunk(sequenceNumber, chunk);

            if (!entry.IsComplete) {
                completeMessage = null;
                return false;
            }

            RemoveEntry(messageID);
            completeMessage = entry.Chunks;
            return true;
        }


        /// <summary>
        /// Removes expired, incomplete chunks from this buffer.
        /// </summary>
        /// <returns>The number of incomplete chunks that have been dropped.</returns>
        public int ExpireAndCleanup(TimeSpan maxAge) {
            var expirationPoint = DateTime.UtcNow.Subtract(maxAge);
            var expired = new List<ulong>();

            _lock.EnterWriteLock();
            try {
                foreach (var entry in _entries) {
                    if (entry.Value.ReceivedAt > expirationPoint)
                        continue;

                    entry.Value.Dispose();

                    expired.Add(entry.Key);
                }

                foreach (var id in expired) {
                    _entries.Remove(id);
                }

                return expired.Count;
            }
            finally {
                _lock.ExitWriteLock();
            }
        }


        public void Clear() {
            _lock.EnterWriteLock();
            try {
                foreach (var entry in _entries.Values) {
                    entry.Dispose();
                }

                _entries.Clear();
            }
            finally {
                _lock.ExitWriteLock();
            }
        }


        // ========================================


        private Entry GetEntry(ulong id, int sequenceCount) {
            Entry result;

            _lock.EnterUpgradeableReadLock();
            try {
                if (_entries.TryGetValue(id, out result))
                    return result;

                result = new Entry(id, sequenceCount, DateTime.UtcNow);

                _lock.EnterWriteLock();
                try {
                    _entries.Add(id, result);
                }
                finally {
                    _lock.ExitWriteLock();
                }
            }
            finally {
                _lock.ExitUpgradeableReadLock();
            }

            return result;
        }


        private void RemoveEntry(ulong id) {
            _lock.EnterWriteLock();
            try {
                _entries.Remove(id);
            }
            finally {
                _lock.ExitWriteLock();
            }
        }


        // ========================================


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadMessageID(byte[] chunk) {
            return ((ulong) chunk[0x2]) |
                   ((ulong) chunk[0x3] << 8) |
                   ((ulong) chunk[0x4] << 16) |
                   ((ulong) chunk[0x5] << 24) |
                   ((ulong) chunk[0x6] << 32) |
                   ((ulong) chunk[0x7] << 40) |
                   ((ulong) chunk[0x8] << 48) |
                   ((ulong) chunk[0x9] << 56);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadChunkIndex(byte[] chunk) {
            return chunk[0xA];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadChunkCount(byte[] chunk) {
            return chunk[0xB];
        }


        // ========================================


        private sealed class Entry : IDisposable {
            private readonly ulong _id;
            private int _chunkCount;

            public RawGelfMessage[] Chunks { get; }

            public DateTime ReceivedAt { get; }


            public bool IsComplete {
                get => _chunkCount == Chunks.Length;
            }


            public Entry(ulong id, int chunkCount, DateTime receivedAt) {
                _id = id;
                Chunks = new RawGelfMessage[chunkCount];
                ReceivedAt = receivedAt;
            }


            public void AddChunk(int sequenceNumber, RawGelfMessage chunk) {
                if (Chunks[sequenceNumber] != null)
                    return;

                Chunks[sequenceNumber] = chunk;
                _chunkCount++;
            }


            public override int GetHashCode() {
                return (int) _id;
            }


            public override bool Equals(object obj) {
                if (ReferenceEquals(this, obj))
                    return true;
                return (obj is Entry entry) && Equals(entry);
            }


            private bool Equals(Entry other) {
                return _id == other._id;
            }


            public void Dispose() {
                foreach (var c in Chunks) {
                    c?.Dispose();
                }
            }
        }
    }
}