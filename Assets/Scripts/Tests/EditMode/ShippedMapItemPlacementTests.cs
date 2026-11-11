using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Data.Persistence;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Loose items in shipped maps load where they are saved. One below the deck spawns clipped into the floor, and one
    /// over an empty cell drops into space (#212: the classic-ministation importer saved scene-local positions).
    /// </summary>
    public sealed class ShippedMapItemPlacementTests
    {
        private static readonly string[] ShippedMapFolders =
        {
            "Builds/Game/Data/StationTemplates",
            "Testing/fixtures/stations",
        };

        // Small items rest a little below their pivot; anything lower is inside the floor.
        private const float LowestItemHeight = -0.05f;

        [Test]
        public void ShippedMaps_PlaceEveryItemAboveTheDeckOverATile()
        {
            var problems = new List<string>();
            int checkedItems = 0;

            foreach (string path in ShippedMapFolders.SelectMany(folder => Directory.GetFiles(folder, "*.json")))
            {
                var envelope = JsonUtility.FromJson<PersistenceEnvelope>(File.ReadAllText(path));
                PersistenceChunk tilemap = envelope.chunks.SingleOrDefault(chunk => chunk.contributorId == "tilemap");
                if (tilemap == null)
                    continue;

                var map = JsonUtility.FromJson<SavedTileMap>(tilemap.payloadJson);
                var occupied = new HashSet<Vector2Int>();
                foreach (SavedTileChunk chunk in map.savedChunkList)
                {
                    foreach (ISavedTileLocation tile in chunk.savedTiles.Where(tile => tile != null && tile.GetPlacedObjects().Count > 0))
                    {
                        Vector3 world = chunk.originPosition + new Vector3(tile.Location.x, 0, tile.Location.y);
                        occupied.Add(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z)));
                    }
                }

                foreach (SavedPlacedItemObject item in map.savedItemList ?? new SavedPlacedItemObject[0])
                {
                    checkedItems++;
                    Vector3 position = item.worldPosition;
                    if (position.y < LowestItemHeight)
                        problems.Add($"{Path.GetFileName(path)}: {item.itemName} at {position} is below the deck");

                    if (!occupied.Contains(new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z))))
                        problems.Add($"{Path.GetFileName(path)}: {item.itemName} at {position} is over an empty cell");
                }
            }

            Assert.Greater(checkedItems, 0, "found items in shipped maps");
            Assert.IsEmpty(problems);
        }
    }
}
