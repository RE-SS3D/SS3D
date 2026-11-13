using NUnit.Framework;
using SS3D.Systems.Furniture;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The nuclear device's sounds: every clip is registered and mono (positional), the countdown beep speeds
    /// up toward zero, and the station-wide alarm sounds once in the last ten seconds.
    /// </summary>
    public class NukeAudioTests
    {
        [Test]
        public void EveryNukeClip_IsRegistered_AndMono()
        {
            SS3D.Data.AssetDatabases.AssetDatabase sounds =
                AssetDatabase.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Sounds.asset");
            foreach (string id in NukeAudioTrackIds.All)
            {
                Assert.IsTrue(sounds.Assets.ContainsKey(id), $"{AssetDatabase.GUIDToAssetPath(id)} is registered in Sounds.asset");
                Assert.That(sounds.Assets[id], Is.TypeOf<AudioClip>());
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));
                Assert.IsTrue(importer.forceToMono, $"{importer.assetPath} imports mono, so it plays positionally");
            }
        }

        [TestCase(180f, 2f)]
        [TestCase(31f, 2f)]
        [TestCase(30f, 1f)]
        [TestCase(11f, 1f)]
        [TestCase(10f, 0.5f)]
        [TestCase(0.2f, 0.5f)]
        public void Beep_SpeedsUpTowardZero(float remaining, float interval)
        {
            Assert.AreEqual(interval, NukeDeviceState.TickInterval(remaining));
        }

        [Test]
        public void Alarm_SoundsOnce_InTheLastTenSeconds()
        {
            NukeDeviceState armed = default(NukeDeviceState).WithDisk(true).ArmedAt(0d, 180f);

            Assert.IsFalse(armed.AlarmDue(169.9d, alarmSent: false));
            Assert.IsTrue(armed.AlarmDue(170d, alarmSent: false));
            Assert.IsFalse(armed.AlarmDue(175d, alarmSent: true), "Once per arming");
            Assert.IsFalse(armed.WithPanelOpen().Defused().AlarmDue(175d, alarmSent: false), "Not once defused");
        }
    }
}
