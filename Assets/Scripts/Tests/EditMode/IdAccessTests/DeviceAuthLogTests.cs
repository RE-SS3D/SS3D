using NUnit.Framework;
using SS3D.Systems.IdAccess;

namespace EditorTests
{
    public class DeviceAuthLogTests
    {
        [Test]
        public void Append_KeepsNewestEntriesFirstWhenQueried()
        {
            var log = new DeviceAuthLog(capacity: 2);
            log.Append(MakeEntry("first", passed: true));
            log.Append(MakeEntry("second", passed: false));
            log.Append(MakeEntry("third", passed: true));

            var entries = log.GetEntriesNewestFirst();

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("third", entries[0].RequesterName);
            Assert.AreEqual("second", entries[1].RequesterName);
        }

        [Test]
        public void Append_RecordsPassAndFail()
        {
            var log = new DeviceAuthLog();
            log.Append(MakeEntry("alice", passed: true));
            log.Append(MakeEntry("bob", passed: false));

            var entries = log.GetEntriesNewestFirst();

            Assert.IsTrue(entries[1].Passed);
            Assert.IsFalse(entries[0].Passed);
        }

        private static AuthLogEntry MakeEntry(string name, bool passed) =>
            new(
                timestamp: 1,
                deviceId: "locker-1",
                requesterRecordId: new CrewRecordId(7),
                requesterName: name,
                requiredAccess: AccessMask.FromLevels(AccessLevel.Security),
                passed: passed);
    }
}
