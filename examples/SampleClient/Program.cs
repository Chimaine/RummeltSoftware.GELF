using System;
using RummeltSoftware.Gelf;
using RummeltSoftware.Gelf.Client;

namespace SampleClient {
    class Program {
        static void Main(string[] args) {
            using (var client = new UdpGelfClient("127.0.0.1", 25588, new GelfClientSettings { })) {
                for (;;) {
                    Console.Write("Enter message to send: ");
                    var input = Console.ReadLine();

                    var message = new GelfMessageBuilder(Environment.MachineName, input).Build();
                    client.Send(message);

                    Console.WriteLine("Message send!");
                }
            }
        }
    }
}