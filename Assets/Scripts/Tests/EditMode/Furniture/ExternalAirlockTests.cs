using NUnit.Framework;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The external airlock (<c>ExternalAirlockSetup</c>, from <c>Firelock.fbx</c>): it seals like any door, opens only
    /// by hand, and its solid collider rides the sinking door, blocking the doorway shut and leaving it walkable open.
    /// </summary>
    public sealed class ExternalAirlockTests
    {
        private const string Doors = "Assets/Content/WorldObjects/Structures/Doors/";
        private const string PrefabPath = Doors + "ExternalAirlock.prefab";
        private const string TilePath = "Assets/Content/Data/TileMap/Resources/Structures/Doors/AirlockExternal.asset";

        [Test]
        public void Tile_IsAnAirtightDoorThatBlocksSight()
        {
            TileObjectSo tile = AssetDatabase.LoadAssetAtPath<TileObjectSo>(TilePath);
            Assert.That(tile, Is.Not.Null);
            Assert.That(tile.NameString, Is.EqualTo("ExternalAirlock"));
            Assert.That(tile.genericType, Is.EqualTo(TileObjectGenericType.Door));
            Assert.That(tile.airtight, Is.True);
            Assert.That(tile.blocksVision, Is.True);
            Assert.That(AssetDatabase.GUIDToAssetPath(tile.PrefabAsset.Id), Is.EqualTo(PrefabPath));
        }

        [Test]
        public void Opener_OpensOnlyByHandAndClosesItself()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            SerializedObject opener = new(prefab.GetComponent<AirLockOpener>());
            Assert.That(opener.FindProperty("_opensOnApproach").boolValue, Is.False, "walking up must not open it");
            Assert.That(opener.FindProperty("_autoCloseDelay").floatValue, Is.GreaterThan(2f), "it outlasts its own 2 s opening");
            Assert.That(AssetDatabase.GUIDToAssetPath(opener.FindProperty("_openSoundId").stringValue), Does.EndWith("ExternalAirlockOpen.ogg"));
            Assert.That(AssetDatabase.GUIDToAssetPath(opener.FindProperty("_closeSoundId").stringValue), Does.EndWith("ExternalAirlockClose.ogg"));

            Animator animator = prefab.GetComponent<Animator>();
            Assert.That(AirLockOpener.PanelMotionTimeFor(animator.runtimeAnimatorController), Is.GreaterThan(2f),
                "an off-camera door keeps animating until the sinking door is down");
        }

        [Test]
        public void Blocker_ShutsTheDoorwayClosedAndSinksOutOfItOpen()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AnimationClip opening = AssetDatabase.LoadAssetAtPath<AnimationClip>(Doors + "ExternalAirlockOpening.anim");
            AnimationClip closing = AssetDatabase.LoadAssetAtPath<AnimationClip>(Doors + "ExternalAirlockClosing.anim");
            GameObject door = Object.Instantiate(prefab);
            try
            {
                BoxCollider blocker = door.transform.Find("Model/FirelockArmature/Bottom2/Bottom/Top/Blocker").GetComponent<BoxCollider>();
                Assert.That(blocker.isTrigger, Is.False);

                opening.SampleAnimation(door, 0f);
                Assert.That(Bottom(blocker), Is.LessThan(0.05f), "closed, it reaches the floor");
                Assert.That(Top(blocker), Is.GreaterThan(1.9f), "closed, it fills the door height");

                opening.SampleAnimation(door, opening.length);
                Assert.That(Top(blocker), Is.LessThan(0.15f), "open, only a low lip is left to step over");

                closing.SampleAnimation(door, closing.length);
                Assert.That(Top(blocker), Is.GreaterThan(1.9f), "closing ends shut");
            }
            finally
            {
                Object.DestroyImmediate(door);
            }
        }

        private static float Top(BoxCollider box) => box.transform.TransformPoint(box.center + 0.5f * box.size.y * Vector3.up).y;

        private static float Bottom(BoxCollider box) => box.transform.TransformPoint(box.center - 0.5f * box.size.y * Vector3.up).y;
    }
}
