using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Electricity;
using SS3D.Systems.Furniture;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The department airlock skins from <c>SS13Airlocks.fbx</c> (built by <c>AirlockSkinSetup</c>): each must work like
    /// the Civilian door, light its own door-light slot, and glass skins must show their glass and let sight through.
    /// Each skin is one palette family of a solid and a glass door.
    /// </summary>
    public sealed class AirlockSkinTests
    {
        private const string DoorTiles = "Assets/Content/Data/TileMap/Resources/Structures/Doors";

        private static readonly Dictionary<string, AccessLevel> AccessBySkin = new()
        {
            { "Airlock", AccessLevel.None },
            { "CivillianAirlock", AccessLevel.Civilian },
            { "MaintenanceAirlock", AccessLevel.Maintenance },
            { "CommandAirlock", AccessLevel.Command },
            { "SecurityAirlock", AccessLevel.Security },
            { "EngineeringAirlock", AccessLevel.Engineering },
            { "AtmosphericsAirlock", AccessLevel.Engineering },
            { "MedicalAirlock", AccessLevel.Medical },
            { "ChemistryAirlock", AccessLevel.Medical },
            { "GeneticsAirlock", AccessLevel.Medical },
            { "VirologyAirlock", AccessLevel.Medical },
            { "SurgeryAirlock", AccessLevel.Medical },
            { "ScienceAirlock", AccessLevel.Science },
            { "SupplyAirlock", AccessLevel.Cargo },
        };

        [Test]
        public void EverySkin_IsAWorkingDoorLikeTheCivilianAirlock()
        {
            TileObjectSo[] tiles = FamilyTiles();
            TileObjectSo civilian = tiles.Single(tile => tile.NameString == "CivillianAirlock");

            Assert.That(tiles.Length, Is.EqualTo(28), "14 skins, solid and glass");
            foreach (TileObjectSo tile in tiles)
            {
                string name = tile.NameString;
                bool glass = name.Contains("Glass");
                Assert.That(tile.layer, Is.EqualTo(civilian.layer), name);
                Assert.That(tile.genericType, Is.EqualTo(TileObjectGenericType.Door), name);
                Assert.That(tile.airtight, Is.True, name);
                Assert.That(tile.blocksVision, Is.EqualTo(!glass), name + ": glass doors are see-through");

                GameObject prefab = PrefabOf(tile);
                Assert.That(prefab.GetComponent<AirLockOpener>(), Is.Not.Null, name);
                Assert.That(prefab.GetComponent<PlacedTileObject>(), Is.Not.Null, name);

                string skin = glass ? name.Substring(0, name.Length - "Glass".Length) : name;
                Assert.That(AccessBySkin.ContainsKey(skin), Is.True, name + " has no expected access");
                SerializedObject gate = new(prefab.GetComponent<AirLockAccessGate>());
                Assert.That(gate.FindProperty("_fallbackRequiredAccessBits").ulongValue, Is.EqualTo((ulong)AccessBySkin[skin]), name);
            }
        }

        [Test]
        public void EverySkin_LightsTheDoorLightSlotAndShowsItsGlass()
        {
            foreach (TileObjectSo tile in FamilyTiles())
            {
                string name = tile.NameString;
                GameObject prefab = PrefabOf(tile);
                int slot = new SerializedObject(prefab.GetComponent<AirLockOpener>()).FindProperty("_doorLightMaterialIndex").intValue;
                SerializedProperty indicators = new SerializedObject(prefab.GetComponent<ConsumerPowerVisual>()).FindProperty("_panelIndicators");
                for (int i = 0; i < indicators.arraySize; i++)
                    Assert.That(indicators.GetArrayElementAtIndex(i).FindPropertyRelative("MaterialIndex").intValue, Is.EqualTo(slot), name + " power visual");

                foreach (string leafName in new[] { "LeftDoor", "RightDoor" })
                {
                    Transform leaf = prefab.transform.Find(leafName);
                    Mesh mesh = leaf.GetComponent<MeshFilter>().sharedMesh;
                    Material[] materials = leaf.GetComponent<MeshRenderer>().sharedMaterials;
                    string where = name + "/" + leafName;

                    Assert.That(materials.Length, Is.EqualTo(mesh.subMeshCount), where);
                    Assert.That(materials[slot].name, Is.EqualTo("GenericShadeless"), where + " door light material");
                    Assert.That(LowestPoint(mesh, slot), Is.GreaterThan(1.8f), where + ": the light slot holds the lights above the door");
                    if (name.Contains("Glass"))
                    {
                        Assert.That(materials.Select(material => material.name), Does.Contain("PaletteGlass"), where);
                        int glassSlot = System.Array.FindIndex(materials, material => material.name == "PaletteGlass");
                        Assert.That(LowestPoint(mesh, glassSlot), Is.LessThan(0.5f), where + ": the glass slot holds the window");
                    }
                }
            }
        }

        [Test]
        public void EverySkin_IsOneFamilyOfASolidAndAGlassDoor()
        {
            foreach (IGrouping<string, string> family in FamilyTiles()
                         .Select(tile => AssetDatabase.GetAssetPath(tile))
                         .GroupBy(path => System.IO.Path.GetDirectoryName(path)))
            {
                string[] tiles = family.Select(path => AssetDatabase.LoadAssetAtPath<TileObjectSo>(path).NameString).OrderBy(name => name).ToArray();
                Assert.That(family.Key, Does.EndWith("AirlockVariants"), string.Join(", ", tiles));
                Assert.That(tiles.Length, Is.EqualTo(2), family.Key);
                Assert.That(tiles[1], Is.EqualTo(tiles[0] + "Glass"), family.Key + ": the palette labels them Standard and Glass");
            }
        }

        private static float LowestPoint(Mesh mesh, int subMesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.GetTriangles(subMesh);
            Assert.That(triangles.Length, Is.GreaterThan(0), mesh.name + " submesh " + subMesh + " is empty");
            return triangles.Min(index => vertices[index].y);
        }

        private static TileObjectSo[] FamilyTiles() =>
            AssetDatabase.FindAssets("t:TileObjectSo", new[] { DoorTiles })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => System.IO.Path.GetDirectoryName(path).EndsWith("AirlockVariants"))
                .Select(path => AssetDatabase.LoadAssetAtPath<TileObjectSo>(path))
                .ToArray();

        private static GameObject PrefabOf(TileObjectSo tile) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(tile.PrefabAsset.Id));
    }
}
