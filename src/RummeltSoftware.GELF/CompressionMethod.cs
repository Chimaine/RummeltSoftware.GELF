using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    [PublicAPI]
    public enum CompressionMethod {
        None,
        GZIP,
        //ZLIB,
    }
}