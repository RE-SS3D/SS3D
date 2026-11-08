using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Persistence;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Saved maps find placed objects by prefab id, then by name (palette S5). An id survives a rename; the name
    /// covers saves from before schema 2.
    /// </summary>
    public sealed class SavedAssetLookupTests
    {
        private static readonly string[] ShippedMapFolders =
        {
            "Builds/Game/Data/StationTemplates",
            "Testing/fixtures/stations",
        };

        private readonly List<Object> _created = new();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object created in _created)
                Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void Id_FindsARenamedAsset()
        {
            TileObjectSo renamed = Create("SupplyPipe", "guid-supply");

            GenericObjectSo found = Resolve(new[] { renamed }, "guid-supply", "AtmosPipesSupplyOldName");

            Assert.AreSame(renamed, found);
        }

        [Test]
        public void OldSaveWithoutId_LoadsByName()
        {
            TileObjectSo vent = Create("Vent", "guid-vent");
            var saved = JsonUtility.FromJson<SavedPlacedTileObject>("{\"tileObjectSOName\":\"Vent\",\"origin\":{\"x\":1,\"y\":2},\"dir\":0}");

            Assert.IsTrue(string.IsNullOrEmpty(saved.id), "a version 1 save has no id");
            Assert.AreSame(vent, Resolve(new[] { vent }, saved.id, saved.tileObjectSOName));
        }

        [Test]
        public void OldName_GoesThroughRenamedTileObjects()
        {
            TileObjectSo supply = Create("AtmosPipesSupply", "guid-supply");

            Assert.AreSame(supply, Resolve(new[] { supply }, null, "AtmosPipesL1"));
        }

        [Test]
        public void UnknownId_FallsBackToName()
        {
            // A deleted and recreated asset gets a new GUID but keeps its name.
            TileObjectSo recreated = Create("Vent", "guid-new");

            Assert.AreSame(recreated, Resolve(new[] { recreated }, "guid-old", "Vent"));
        }

        [Test]
        public void NeitherIdNorName_Resolves_ReturnsNull()
        {
            TileObjectSo vent = Create("Vent", "guid-vent");

            Assert.IsNull(Resolve(new[] { vent }, "guid-gone", "Gone"));
            Assert.IsNull(Resolve(new[] { vent }, null, null));
        }

        [Test]
        public void SavedItem_WritesIdFirst()
        {
            string json = JsonUtility.ToJson(new SavedPlacedItemObject { id = "guid-disk", itemName = "NuclearAuthenticationDisk" });

            StringAssert.StartsWith("{\"id\":\"guid-disk\",\"itemName\":", json);
        }

        [Test]
        public void SchemaVersion_IsTwo()
        {
            Assert.AreEqual(2, PersistenceEnvelope.CurrentSchemaVersion, "schema 2 adds the placed-object id");
        }

        [Test]
        public void EveryTileAsset_HasAUniquePrefabId()
        {
            GenericObjectSo[] assets = Resources.LoadAll<GenericObjectSo>("");
            Assert.IsNotEmpty(assets);

            foreach (GenericObjectSo asset in assets)
                Assert.IsFalse(string.IsNullOrEmpty(SavedAssetLookup.IdOf(asset)), $"{asset.name} has no prefab id");

            string[] shared = assets.GroupBy(SavedAssetLookup.IdOf).Where(group => group.Count() > 1)
                .Select(group => $"{group.Key}: {string.Join(", ", group.Select(asset => asset.name))}").ToArray();
            Assert.IsEmpty(shared, "tile assets sharing a prefab id");
        }

        [Test]
        public void ShippedMaps_StoreAnIdMatchingEachName()
        {
            Dictionary<string, GenericObjectSo> byId = Resources.LoadAll<GenericObjectSo>("")
                .ToDictionary(SavedAssetLookup.IdOf);
            var problems = new List<string>();
            int checkedMaps = 0;

            foreach (string path in ShippedMapFolders.SelectMany(folder => Directory.GetFiles(folder, "*.json")))
            {
                var envelope = JsonUtility.FromJson<PersistenceEnvelope>(File.ReadAllText(path));
                PersistenceChunk tilemap = envelope.chunks.SingleOrDefault(chunk => chunk.contributorId == "tilemap");
                if (tilemap == null)
                    continue;

                checkedMaps++;
                if (envelope.schemaVersion < PersistenceEnvelope.CurrentSchemaVersion)
                    problems.Add($"{path}: schemaVersion {envelope.schemaVersion}");

                var map = JsonUtility.FromJson<SavedTileMap>(tilemap.payloadJson);
                IEnumerable<(string id, string name)> saved = map.savedChunkList
                    .SelectMany(chunk => chunk.savedTiles.Where(tile => tile != null))
                    .SelectMany(tile => tile.GetPlacedObjects())
                    .Select(placed => (placed.id, placed.tileObjectSOName))
                    .Concat((map.savedItemList ?? new SavedPlacedItemObject[0]).Select(item => (item.id, item.itemName)));

                foreach ((string id, string name) in saved.Distinct())
                {
                    if (string.IsNullOrEmpty(id) || !byId.TryGetValue(id, out GenericObjectSo asset))
                        problems.Add($"{path}: {name} has unknown id '{id}'");
                    else if (asset.NameString != name)
                        problems.Add($"{path}: id {id} is {asset.NameString}, saved as {name}");
                }
            }

            Assert.Greater(checkedMaps, 0, "found shipped maps");
            Assert.IsEmpty(problems, "run Tools/map_import/asset_ids.py on the listed maps");
        }

        private TileObjectSo Create(string name, string id)
        {
            ObjectAssetReference prefab = ScriptableObject.CreateInstance<ObjectAssetReference>();
            prefab.name = name;
            prefab.Id = id;
            prefab.Database = "test";

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.PrefabAsset = prefab;
            _created.Add(prefab);
            _created.Add(so);
            return so;
        }

        private static GenericObjectSo Resolve(IReadOnlyList<GenericObjectSo> assets, string id, string savedName) =>
            SavedAssetLookup.Resolve(id, savedName,
                candidate => assets.FirstOrDefault(asset => SavedAssetLookup.IdOf(asset) == candidate),
                name => assets.FirstOrDefault(asset => string.Equals(asset.NameString, name, System.StringComparison.OrdinalIgnoreCase)));
    }
}
