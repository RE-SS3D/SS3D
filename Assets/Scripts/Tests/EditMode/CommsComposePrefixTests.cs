using NUnit.Framework;
using SS3D.Systems.Comms;
using UnityEngine;

namespace EditorTests
{
    public class CommsComposePrefixTests
    {
        [Test]
        public void TrySplit_PlainText_ReturnsFalse()
        {
            Assert.IsFalse(CommsComposePrefix.TrySplit("hello eng", out _, out string body));
            Assert.AreEqual("hello eng", body);
        }

        [Test]
        public void TrySplit_EngWithBody_StripsToken()
        {
            Assert.IsTrue(CommsComposePrefix.TrySplit("/eng plasma leak", out string token, out string body));
            Assert.AreEqual("eng", token);
            Assert.AreEqual("plasma leak", body);
        }

        [Test]
        public void TrySplit_AnnounceCaseInsensitive()
        {
            Assert.IsTrue(CommsComposePrefix.TrySplit("/ANNOUNCE Red alert", out string token, out string body));
            Assert.AreEqual("announce", token);
            Assert.AreEqual("Red alert", body);
        }

        [Test]
        public void TrySplit_PrefixOnly_EmptyBody()
        {
            Assert.IsTrue(CommsComposePrefix.TrySplit("/sec", out string token, out string body));
            Assert.AreEqual("sec", token);
            Assert.AreEqual(string.Empty, body);
        }

        [Test]
        public void TrySplit_SlashAlone_ReturnsFalse()
        {
            Assert.IsFalse(CommsComposePrefix.TrySplit("/", out _, out _));
        }

        [Test]
        public void TrySplit_Null_ReturnsFalse()
        {
            Assert.IsFalse(CommsComposePrefix.TrySplit(null, out _, out string body));
            Assert.AreEqual(string.Empty, body);
        }

        [Test]
        public void Radio_ResolveComposePrefix_FallsBackToAbbreviation()
        {
            CommsChannel channel = ScriptableObject.CreateInstance<CommsChannel>();
            channel.Kind = CommsChannelKind.Radio;
            channel.Abbreviation = "ENG";
            channel.AvailableInCompose = true;
            channel.CodeOnlyChannel = false;

            Assert.AreEqual("eng", channel.ResolveComposePrefix());
            Assert.IsTrue(channel.IsComposePrefixWritable());
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Announcement_WithoutExplicitPrefix_NotWritable()
        {
            CommsChannel channel = ScriptableObject.CreateInstance<CommsChannel>();
            channel.Kind = CommsChannelKind.Announcement;
            channel.Abbreviation = "STA";
            channel.CodeOnlyChannel = false;

            Assert.IsTrue(string.IsNullOrEmpty(channel.ResolveComposePrefix()));
            Assert.IsFalse(channel.IsComposePrefixWritable());
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Announcement_WithComposePrefix_IsWritableWithoutTabFlag()
        {
            CommsChannel channel = ScriptableObject.CreateInstance<CommsChannel>();
            channel.Kind = CommsChannelKind.Announcement;
            channel.ComposePrefix = "announce";
            channel.AvailableInCompose = false;
            channel.CodeOnlyChannel = false;

            Assert.AreEqual("announce", channel.ResolveComposePrefix());
            Assert.IsTrue(channel.IsComposePrefixWritable());
            Object.DestroyImmediate(channel);
        }
    }
}
