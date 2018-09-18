using JetBrains.Annotations;

namespace RummeltSoftware.Gelf {
    [PublicAPI]
    public sealed class GelfMessageFormatException : GelfException {
        public GelfMessageFormatException(string message)
            : base(message) {}
    }
}