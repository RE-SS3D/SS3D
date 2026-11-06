using NUnit.Framework;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using UnityEditor;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    public class AirAlarmEmissionTests
    {
        private const string PrefabPath = "Assets/Content/WorldObjects/Structures/WallMounts/AirAlarm.prefab";

        [Test]
        public void AirAlarmPrefab_HasAGlowSlotForTheAlarmToDrive()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var slots = new EmissiveMaterialSlots();

            slots.Cache(prefab.GetComponentsInChildren<Renderer>(), AirAlarmController.GlowsWhenPowered);

            Assert.AreEqual(1, slots.Count, "Only the glowing face of the alarm blinks");
        }
    }
}
