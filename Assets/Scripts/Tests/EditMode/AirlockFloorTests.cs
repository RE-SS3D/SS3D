using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Rules for the two half tiles under an airlock (RE-SS3D/SS3D#1299): which floors a half may copy,
    /// which half faces which side, and the world-aligned visual. The live place/clear path is
    /// <c>AirlockFloorScenarios</c>.
    /// </summary>
    public class AirlockFloorTests
    {
        private const string Floors = "Assets/Content/WorldObjects/Structures/Floors/Tiles/";

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
                Object.DestroyImmediate(_instance);
        }

        [TestCase("Wood/TileWood.prefab", "TileWood")]
        [TestCase("Steel/TileGrey.prefab", "TileGrey")]
        [TestCase("Reinforced/TileReinforced.prefab", "TileReinforced")]
        [TestCase("TilePlating.prefab", "TilePlating")]
        public void TexturedFloor_LendsItsMaterialToTheHalfTile(string floorPrefab, string materialName)
        {
            Material material = AirlockFloorSides.GetHalfMaterial(Load(Floors + floorPrefab), Visual().AtlasMaterials);

            Assert.IsNotNull(material);
            Assert.AreEqual(materialName, material.name);
        }

        [TestCase("Dirt/TileDirt1.prefab")]
        [TestCase("Grass/TileGrass.prefab")]
        [TestCase("Carpet/FancyCarpetRed.prefab")]
        public void AtlasOrConnectedFloor_LeavesTheHalfOpen(string floorPrefab)
        {
            Assert.IsNull(AirlockFloorSides.GetHalfMaterial(Load(Floors + floorPrefab), Visual().AtlasMaterials));
        }

        [TestCase(Direction.North, true)]
        [TestCase(Direction.South, true)]
        [TestCase(Direction.East, false)]
        [TestCase(Direction.West, false)]
        public void DoorAxis_PicksTheMeshWhoseHalvesLieFrontAndBack(Direction door, bool northSouth)
        {
            Assert.AreEqual(northSouth, AirlockFloorSides.UsesNorthSouthMesh(door));
        }

        [TestCase(Direction.North, 0)]
        [TestCase(Direction.East, 0)]
        [TestCase(Direction.South, 1)]
        [TestCase(Direction.West, 1)]
        public void Submesh_TowardEachSide(Direction side, int submesh)
        {
            Assert.AreEqual(submesh, AirlockFloorSides.SubmeshToward(side));
        }

        [Test]
        public void HalfMeshes_LieOnTheSideTheirSubmeshNames()
        {
            AirlockFloorVisual visual = Visual();
            var northSouth = new SerializedObject(visual).FindProperty("northSouthTile").objectReferenceValue as MeshRenderer;
            var eastWest = new SerializedObject(visual).FindProperty("eastWestTile").objectReferenceValue as MeshRenderer;
            Mesh ns = northSouth.GetComponent<MeshFilter>().sharedMesh;
            Mesh ew = eastWest.GetComponent<MeshFilter>().sharedMesh;

            Assert.Greater(AirlockFloorSides.GetHalfMesh(ns, 0).bounds.center.z, 0f, "NS submesh 0 is the north half");
            Assert.Less(AirlockFloorSides.GetHalfMesh(ns, 1).bounds.center.z, 0f, "NS submesh 1 is the south half");
            Assert.Greater(AirlockFloorSides.GetHalfMesh(ew, 0).bounds.center.x, 0f, "EW submesh 0 is the east half");
            Assert.Less(AirlockFloorSides.GetHalfMesh(ew, 1).bounds.center.x, 0f, "EW submesh 1 is the west half");
        }

        [Test]
        public void Visual_StaysWorldAligned_AndShowsOnlyTheDressedHalves()
        {
            var door = new GameObject("Door");
            _instance = door;
            door.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            GameObject floor = Object.Instantiate(Load(Floors + "AirlockTile.prefab"), door.transform);
            AirlockFloorVisual visual = floor.GetComponent<AirlockFloorVisual>();
            Material wood = AssetDatabase.LoadAssetAtPath<Material>(Floors + "Wood/TileWood.mat");

            visual.Apply(northSouth: false, wood, null);

            Assert.Less(Quaternion.Angle(Quaternion.identity, floor.transform.rotation), 0.01f, "Wood grain does not turn with the door");
            Assert.IsTrue(floor.transform.Find("AirlockTrackEW").gameObject.activeSelf, "The track runs along the door");
            Assert.IsFalse(floor.transform.Find("AirlockTrackNS").gameObject.activeSelf);
            Assert.IsTrue(visual.GetHalf(0).gameObject.activeSelf);
            Assert.AreSame(wood, visual.GetHalf(0).sharedMaterial);
            Assert.IsFalse(visual.GetHalf(1).gameObject.activeSelf, "A half with nothing to copy stays open");
        }

        private static AirlockFloorVisual Visual() =>
            Load(Floors + "AirlockTile.prefab").GetComponent<AirlockFloorVisual>();

        private static GameObject Load(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            return prefab;
        }
    }
}
