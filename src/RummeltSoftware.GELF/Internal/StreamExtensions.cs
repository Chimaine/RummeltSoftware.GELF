using System.IO;

namespace RummeltSoftware.Gelf.Internal {
    internal static class StreamExtensions {
        /// <summary>
        /// Uses a <see cref="ByteArrayPool"/> buffer to do the copying.
        /// </summary>
        /// <param name="src">The stream to copy from</param>
        /// <param name="dst">The stream to copy to</param>
        /// <param name="bufferSize">Size of the buffer to use</param>
        public static void CopyToPooled(this Stream src, Stream dst, int bufferSize = 81920) {
            var buffer = ByteArrayPool.Rent(bufferSize);
            try {
                int count;
                do {
                    count = src.Read(buffer, 0, buffer.Length);
                    dst.Write(buffer, 0, count);
                }
                while (count > 0);
            }
            finally {
                ByteArrayPool.Return(buffer);
            }
        }
    }
}