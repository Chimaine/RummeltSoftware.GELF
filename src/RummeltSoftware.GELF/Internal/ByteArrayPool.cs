using System.Buffers;

namespace RummeltSoftware.Gelf.Internal {
    internal static class ByteArrayPool {
        private static readonly ArrayPool<byte> Pool;

        // ========================================


        static ByteArrayPool() {
            Pool = ArrayPool<byte>.Shared;
        }


        // ========================================


        public static byte[] Rent(int minimumLength) {
            return Pool.Rent(minimumLength);
        }


        public static void Return(byte[] array, bool clearArray = false) {
            Pool.Return(array, clearArray);
        }
    }
}