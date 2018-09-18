namespace RummeltSoftware.Gelf.Listener.Internal {
    internal sealed class TooManyChunksException : GelfException {
        public TooManyChunksException(string message)
            : base(message) {}
    }
}