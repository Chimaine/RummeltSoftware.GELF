using System.IO;
using Microsoft.IO;

namespace RummeltSoftware.Gelf.Internal {
    internal static class MemoryStreamPool {
        private static readonly RecyclableMemoryStreamManager Pool;

        // ========================================


        static MemoryStreamPool() {
            Pool = new RecyclableMemoryStreamManager(32768, 1048576, 8388608) {
                AggressiveBufferReturn = true,
                GenerateCallStacks = false,
            };
        }


        // ========================================


        public static MemoryStream GetStream() {
            return Pool.GetStream();
        }


        public static MemoryStream GetStream(string tag) {
            return Pool.GetStream(tag);
        }
    }
}