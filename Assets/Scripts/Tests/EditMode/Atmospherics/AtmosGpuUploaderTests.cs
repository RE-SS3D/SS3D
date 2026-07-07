using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosGpuUploaderTests
    {
        private List<GameObject> _instantiated;

        [SetUp]
        public void SetUp() => _instantiated = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        [Test]
        public void GpuUploader_PressureAtlasMatchesSimulation()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            BuildPlenumTile(context, 1, 1);

            using var simulation = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });
            simulation.UpdateCell(new TileCoord(context.Map.MapId, 1, 1));

            using var uploader = new AtmosGpuUploader();
            uploader.Refresh(simulation);

            Assert.IsTrue(uploader.IsValid);
            var snapshot = uploader.BuildSnapshot();
            Assert.IsTrue(snapshot.Valid);

            var coord = new TileCoord(context.Map.MapId, 1, 1);
            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));

            int localX = coord.Grid.x - (int)snapshot.AtlasBounds.x;
            int localZ = coord.Grid.y - (int)snapshot.AtlasBounds.y;
            int atlasWidth = (int)snapshot.AtlasBounds.z;
            int index = localZ * atlasWidth + localX;

            var pressureData = snapshot.Pressure.GetPixelData<float>(0);
            Assert.That(pressureData[index], Is.EqualTo(info.Pressure).Within(0.01f));
        }

        private static void BuildPlenumTile(TileMapTestUtilities.MapContext context, int x, int z)
        {
            TileMapTestUtilities.PlacePlenum(context, new Vector3(x, 0, z));
        }
    }
}
