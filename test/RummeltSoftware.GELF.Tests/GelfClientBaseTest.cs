using Microsoft.VisualStudio.TestTools.UnitTesting;
using RummeltSoftware.Gelf.Client;
using RummeltSoftware.Gelf.Listener.Internal;
using RummeltSoftware.Gelf.Mocks;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class GelfClientBaseTest {
        [TestMethod]
        public void TestSendSmallUncompressedMessage() {
            var client = new MockGelfClient(new GelfClientSettings(), false, false);
            var originalMessage = new GelfMessageBuilder("localhost", "TestMessage").Build();

            client.Send(originalMessage);
            var sendMessage = new GelfMessageDecoder().Decode(client.LastSendMessage)?.Message;

            Assert.IsNotNull(sendMessage);
            Assert.That.MessagesAreEqual(originalMessage, sendMessage);
        }
    }
}