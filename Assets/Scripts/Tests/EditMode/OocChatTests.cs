using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Comms;

namespace EditorTests
{
    public class OocChatTests
    {
        [Test]
        public void TryNormalize_RejectsBlank()
        {
            Assert.IsFalse(OocChatRules.TryNormalize(null, out _));
            Assert.IsFalse(OocChatRules.TryNormalize(" \n\t ", out _));
            Assert.IsFalse(OocChatRules.TryNormalize("\u0001\u0002", out _));
        }

        [Test]
        public void TryNormalize_CollapsesWhitespaceAndControls()
        {
            Assert.IsTrue(OocChatRules.TryNormalize("  hello\r\n\tthere \u0007 crew  ", out string text));
            Assert.AreEqual("hello there crew", text);
        }

        [Test]
        public void TryNormalize_ClampsLength()
        {
            Assert.IsTrue(OocChatRules.TryNormalize(new string('a', OocChatRules.MaxLength + 50), out string text));
            Assert.AreEqual(OocChatRules.MaxLength, text.Length);
        }

        [Test]
        public void IsValidChannel_RejectsUnknownValues()
        {
            Assert.IsTrue(OocChatRules.IsValidChannel(OocChannel.Admin));
            Assert.IsFalse(OocChatRules.IsValidChannel((OocChannel)42));
        }

        [Test]
        public void KeepsHistory_ExcludesLooc()
        {
            Assert.IsTrue(OocChatRules.KeepsHistory(OocChannel.Ooc));
            Assert.IsTrue(OocChatRules.KeepsHistory(OocChannel.Admin));
            Assert.IsFalse(OocChatRules.KeepsHistory(OocChannel.Looc));
        }

        [Test]
        public void RateLimiter_BlocksBurstUntilWindowPasses()
        {
            OocRateLimiter limiter = new();
            for (int i = 0; i < OocRateLimiter.MaxMessages; i++)
                Assert.IsTrue(limiter.TryConsume("alice", 0f + i * 0.1f));

            Assert.IsFalse(limiter.TryConsume("alice", 1f));
            Assert.IsTrue(limiter.TryConsume("bob", 1f), "Limits are per sender");
            Assert.IsTrue(limiter.TryConsume("alice", OocRateLimiter.WindowSeconds + 0.01f));
        }

        [Test]
        public void History_DropsOldestBeyondCapacity()
        {
            OocChatHistory history = new(3);
            for (long id = 1; id <= 5; id++) Assert.IsTrue(history.Add(Line(id)));

            CollectionAssert.AreEqual(new long[] { 3, 4, 5 }, history.Messages.Select(m => m.Id).ToArray());
            Assert.AreEqual(5, history.LastId);
        }

        [Test]
        public void History_ReplayAfterLiveLinesStaysOrderedAndDeduped()
        {
            OocChatHistory history = new(10);
            history.Add(Line(4));
            history.Add(Line(5));

            // The history replay arrives after live lines that were broadcast first.
            foreach (long id in new long[] { 1, 2, 3, 4 }) history.Add(Line(id));

            CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4, 5 }, history.Messages.Select(m => m.Id).ToArray());
            Assert.IsFalse(history.Add(Line(5)), "Duplicate id is ignored");
        }

        [Test]
        public void History_FullLogIgnoresOlderReplay()
        {
            OocChatHistory history = new(2);
            history.Add(Line(8));
            history.Add(Line(9));

            Assert.IsFalse(history.Add(Line(1)));
            CollectionAssert.AreEqual(new long[] { 8, 9 }, history.Messages.Select(m => m.Id).ToArray());
        }

        private static OocChatMessage Line(long id) => new() { Id = id, Channel = OocChannel.Ooc, Sender = "x", Text = $"line {id}" };
    }
}
