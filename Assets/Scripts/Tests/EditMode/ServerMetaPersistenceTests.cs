using NUnit.Framework;
using SS3D.Data.Persistence;
using SS3D.Systems.Persistence;
using System.IO;
using UnityEngine;

namespace EditorTests
{
    public class ServerMetaPersistenceTests
    {
        [Test]
        public void LegacyPermissionsMigrator_ParsesTxtLines()
        {
            string tempDir = Path.Combine(Application.temporaryCachePath, "ServerMetaPersistenceTests");
            Directory.CreateDirectory(tempDir);
            string permissionsPath = Path.Combine(tempDir, "permissions.txt");
            File.WriteAllText(permissionsPath, "alice Administrator\nbob User\n");

            Assert.IsTrue(LegacyPermissionsMigrator.TryLoadFromLegacyTxtAtPath(permissionsPath, out SavedPermissionsPayload payload));
            Assert.AreEqual(2, payload.records.Length);
            Assert.AreEqual("alice", payload.records[0].ckey);
            Assert.AreEqual("Administrator", payload.records[0].role);
        }

        [Test]
        public void RoundHistoryStore_AppendsJsonlLine()
        {
            var entry = new RoundHistoryEntry
            {
                timestamp = "2026-07-14T12:00:00Z",
                gamemode = "Secret",
                mapId = "Outpost",
                playerCount = 12,
                durationSeconds = 3600,
                fallbackMap = true,
            };

            Assert.IsTrue(RoundHistoryStore.Append(entry));
        }
    }
}
