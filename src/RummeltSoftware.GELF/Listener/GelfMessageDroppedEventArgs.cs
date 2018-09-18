using JetBrains.Annotations;

namespace RummeltSoftware.Gelf.Listener {
    [PublicAPI]
    public sealed class GelfMessageDroppedEventArgs {
        public string Reason { get; set; }

        // ========================================


        public GelfMessageDroppedEventArgs(string reason) {
            Reason = reason;
        }
    }
}