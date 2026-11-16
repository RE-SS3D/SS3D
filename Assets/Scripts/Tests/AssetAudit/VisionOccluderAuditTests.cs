using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Editor;
using UnityEngine;

namespace SS3D.Tests.AssetAudit
{
    /// <summary>
    /// Sight blockers match the tiles and the art (2026-11_fov-rework D3): every wall-type tile that blocks sight
    /// has a <see cref="VisionOccluder"/> as thick as its mesh at eye height, windows and bare girders have none,
    /// and every door leaf carries a box the size of its mesh. Fix drift with
    /// SS3D → Rendering → Run Content Prefab Recipes.
    /// </summary>
    public sealed class VisionOccluderAuditTests
    {
        [Test]
        public void SightOccluders_MatchTilesAndMeshes()
        {
            Assert.GreaterOrEqual(VisionOccluder.Layer, 0, $"The {VisionOccluder.LayerName} layer exists");
            for (int layer = 0; layer < 32; layer++)
            {
                Assert.IsTrue(Physics.GetIgnoreLayerCollision(VisionOccluder.Layer, layer),
                    $"Sight occluders only answer queries; they must not collide with {LayerMask.LayerToName(layer)} ({layer})");
            }

            List<string> drift = VisionOccluderRecipe.Drift();
            Assert.IsEmpty(drift, "Run SS3D → Rendering → Run Content Prefab Recipes:\n" + string.Join("\n", drift));
        }
    }
}
