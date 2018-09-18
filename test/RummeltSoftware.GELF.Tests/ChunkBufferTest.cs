using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RummeltSoftware.Gelf.Listener.Internal;

namespace RummeltSoftware.Gelf {
    [TestClass]
    public class ChunkBufferTest {
        [TestMethod]
        public void TestSingleChunk() {
            var chunk = GenerateChunk(1, 0, 1, 1024);
            var buffer = new ChunkBuffer();

            var completed = buffer.TryCompleteMessage(chunk, out var completeMessage);
            Assert.AreEqual(0, buffer.Count);

            Assert.IsTrue(completed);
            Assert.AreEqual(1, completeMessage.Length);
            Assert.IsNotNull(completeMessage[0]);
        }


        [TestMethod]
        public void TestTwoChunks() {
            var chunk1 = GenerateChunk(1, 0, 2, 1024);
            var chunk2 = GenerateChunk(1, 1, 2, 1024);

            bool completed;
            RawGelfMessage[] completeMessage;

            var buffer = new ChunkBuffer();

            completed = buffer.TryCompleteMessage(chunk1, out completeMessage);
            Assert.AreEqual(1, buffer.Count);

            Assert.IsFalse(completed);
            Assert.IsNull(completeMessage);

            completed = buffer.TryCompleteMessage(chunk2, out completeMessage);
            Assert.AreEqual(0, buffer.Count);

            Assert.IsTrue(completed);
            Assert.AreEqual(2, completeMessage.Length);
            Assert.IsNotNull(completeMessage[0]);
            Assert.IsNotNull(completeMessage[1]);
        }


        [TestMethod]
        public void TestTooManyChunks() {
            var chunk = GenerateChunk(1, 0, 129, 1024);
            var buffer = new ChunkBuffer();

            Assert.ThrowsException<TooManyChunksException>(() => buffer.TryCompleteMessage(chunk, out _));
        }


        [TestMethod]
        public void TestMultipleIDs() {
            var chunk1_1 = GenerateChunk(1, 0, 2, 1024);
            var chunk1_2 = GenerateChunk(1, 1, 2, 1024);

            var chunk2_1 = GenerateChunk(2, 0, 2, 1024);
            var chunk2_2 = GenerateChunk(2, 1, 2, 1024);

            bool completed1, completed2;
            RawGelfMessage[] completeMessage1, completeMessage2;

            var buffer = new ChunkBuffer();

            completed1 = buffer.TryCompleteMessage(chunk1_1, out completeMessage1);
            completed2 = buffer.TryCompleteMessage(chunk2_1, out completeMessage2);

            Assert.IsFalse(completed1);
            Assert.IsFalse(completed2);
            Assert.AreEqual(2, buffer.Count);

            completed1 = buffer.TryCompleteMessage(chunk1_2, out completeMessage1);
            Assert.AreEqual(1, buffer.Count);

            Assert.IsTrue(completed1);
            Assert.AreEqual(2, completeMessage1.Length);
            Assert.IsNotNull(completeMessage1[0]);
            Assert.IsNotNull(completeMessage1[1]);

            completed2 = buffer.TryCompleteMessage(chunk2_2, out completeMessage2);
            Assert.AreEqual(0, buffer.Count);

            Assert.IsTrue(completed2);
            Assert.AreEqual(2, completeMessage2.Length);
            Assert.IsNotNull(completeMessage2[0]);
            Assert.IsNotNull(completeMessage2[1]);
        }


        [TestMethod]
        public void TestExpireAndCleanup_Expiration() {
            var chunk1 = GenerateChunk(1, 0, 2, 1024, DateTime.MinValue);
            var chunk2 = GenerateChunk(1, 1, 2, 1024, DateTime.MinValue);

            bool completed;
            RawGelfMessage[] completeMessage;

            var buffer = new ChunkBuffer();

            completed = buffer.TryCompleteMessage(chunk1, out completeMessage);
            Assert.IsFalse(completed);
            Assert.IsNull(completeMessage);

            var expired = buffer.ExpireAndCleanup(TimeSpan.FromSeconds(0));
            Assert.AreEqual(1, expired);
            Assert.AreEqual(0, buffer.Count);

            completed = buffer.TryCompleteMessage(chunk2, out completeMessage);
            Assert.IsFalse(completed);
            Assert.IsNull(completeMessage);
        }


        [TestMethod]
        public void TestExpireAndCleanup_Keeping() {
            var chunk1 = GenerateChunk(1, 0, 2, 1024);
            var chunk2 = GenerateChunk(1, 1, 2, 1024);

            bool completed;
            RawGelfMessage[] completeMessage;

            var buffer = new ChunkBuffer();

            completed = buffer.TryCompleteMessage(chunk1, out completeMessage);
            Assert.IsFalse(completed);
            Assert.IsNull(completeMessage);

            var expired = buffer.ExpireAndCleanup(TimeSpan.FromDays(9001));
            Assert.AreEqual(0, expired);
            Assert.AreEqual(1, buffer.Count);

            completed = buffer.TryCompleteMessage(chunk2, out completeMessage);
            Assert.IsTrue(completed);
        }


        [TestMethod]
        public void TestClear() {
            var chunk1 = GenerateChunk(1, 0, 2, 1024);
            var chunk2 = GenerateChunk(2, 0, 2, 1024);

            var buffer = new ChunkBuffer();

            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);

            buffer.TryCompleteMessage(chunk1, out _);
            buffer.TryCompleteMessage(chunk2, out _);

            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);
        }


        private static RawGelfMessage GenerateChunk(long messageID, int index, int count, int length, DateTime? timestamp = null) {
            var bytes = new byte[length];

            bytes[0x0] = 0x1E;
            bytes[0x1] = 0x0F;
            bytes[0x2] = (byte) messageID;
            bytes[0x3] = (byte) (messageID >> 8);
            bytes[0x4] = (byte) (messageID >> 16);
            bytes[0x5] = (byte) (messageID >> 24);
            bytes[0x6] = (byte) (messageID >> 32);
            bytes[0x7] = (byte) (messageID >> 40);
            bytes[0x8] = (byte) (messageID >> 48);
            bytes[0x9] = (byte) (messageID >> 56);
            bytes[0xA] = (byte) index;
            bytes[0xB] = (byte) count;

            return new RawGelfMessage(bytes, length, new IPEndPoint(IPAddress.Any, 0), timestamp ?? DateTime.UtcNow);
        }
    }
}