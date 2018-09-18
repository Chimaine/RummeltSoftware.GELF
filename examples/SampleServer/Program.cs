using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using RummeltSoftware.Gelf.Listener;

namespace SampleServer {
    class Program {
        static void Main(string[] args) {
            var localEndpoint = new IPEndPoint(IPAddress.Any, 25588);
            var listener = new UdpGelfListener(new GelfListenerSettings { });

            listener.MessageReceived += OnMessageReceived;
            listener.MessageDropped += OnMessageDropped;
            listener.ReadException += OnReadException;

            Console.WriteLine($"Beginning to listen on {localEndpoint}");
            using (listener) {
                listener.Open(localEndpoint);

                Console.WriteLine("Listing. Press any key to stop...");
                Console.ReadKey(true);
            }
        }


        private static readonly Regex AdditionalFieldReplacePattern = new Regex(@"_(\w)", RegexOptions.Compiled);


        private static void OnMessageReceived(object sender, GelfMessageReceivedEventArgs e) {
            var msg = new StringBuilder()
                      .AppendLine("Message received!")
                      .Append("- Message: ").Append(e.Message.FullMessage ?? e.Message.ShortMessage).AppendLine()
                      .Append("- Level: ").Append(e.Message.Level).AppendLine()
                      .Append("- Timestamp: ").Append(e.Message.Timestamp?.ToString("G")).AppendLine()
                      .Append("- Host: ").Append(e.Message.Host).AppendLine();

            foreach (var field in e.Message.AdditionalFields.OrderBy(x => x.Key, StringComparer.InvariantCultureIgnoreCase)) {
                var key = AdditionalFieldReplacePattern.Replace(field.Key, m => m.Groups[1].Value.ToUpperInvariant());

                msg.Append("- ").Append(key).Append(": ").Append(field.Value).AppendLine();
            }

            Console.Write(msg.ToString());
        }


        private static void OnMessageDropped(object sender, GelfMessageDroppedEventArgs e) {
            Console.WriteLine("Message dropped! " + e.Reason);
        }


        private static void OnReadException(object sender, GelfMessageReadExceptionEventArgs e) {
            Console.WriteLine("Read Exception! " + e.Exception);
        }
    }
}