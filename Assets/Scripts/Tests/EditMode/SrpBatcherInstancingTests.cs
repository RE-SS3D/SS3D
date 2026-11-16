using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Rendering.URP;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EditorTests
{
    public class SrpBatcherInstancingTests : EditModeTest
    {
        [Test]
        public void FloorStMaterials_HaveGpuInstancingEnabled()
        {
            string[] paths =
            {
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Steel/TileGrey.mat",
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Steel/TileGreyDark.mat",
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Steel/TileBar.mat",
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Steel/TileKitchen.mat",
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Wood/TileWood.mat",
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Reinforced/TileReinforced.mat",
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/TilePlating.mat",
            };

            foreach (string path in paths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(material, path);
                Assert.IsTrue(
                    material.enableInstancing,
                    $"{path} must have GPU Instancing enabled");
            }
        }

        [Test]
        public void StationStructurePrefabs_HaveLightAndReflectionProbesOff()
        {
            string[] paths =
            {
                "Assets/Content/WorldObjects/Structures/Floors/Tiles/Steel/TileGrey.prefab",
                "Assets/Content/WorldObjects/Structures/Walls/SteelWall.prefab",
                "Assets/Content/WorldObjects/Structures/Walls/SteelWindow.prefab",
                "Assets/Content/WorldObjects/Structures/Doors/CivillianAirlock.prefab",
            };

            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(renderers.Length, 0, $"{path} should have renderers");

                foreach (Renderer renderer in renderers)
                {
                    Assert.AreEqual(
                        LightProbeUsage.Off,
                        renderer.lightProbeUsage,
                        $"{path} / {renderer.name}: light probes must be Off (SS3D/Rendering/Run Content Prefab Recipes)");
                    Assert.AreEqual(
                        ReflectionProbeUsage.Off,
                        renderer.reflectionProbeUsage,
                        $"{path} / {renderer.name}: reflection probes must be Off");
                }
            }
        }

        [Test]
        public void TileAdjacencyView_ApplyConnections_AssignsSharedMesh()
        {
            Mesh mesh = new Mesh { name = "AdjacencySharedMesh" };

            GameObject go = new GameObject("AdjacencyView");
            instantiated.Add(go);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = new Mesh { name = "Placeholder" };

            TileAdjacencyView view = go.AddComponent<TileAdjacencyView>();
            view.Configure(new StubMeshResolver(mesh));
            view.ApplyConnections(0);

            Assert.AreSame(mesh, filter.sharedMesh, "Adjacency must assign sharedMesh (not .mesh clone)");
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Selectable_MeshRenderer_CollectPickDrawsWithoutPermanentSelectionMpb()
        {
            Mesh mesh = new Mesh { name = "PickMesh" };
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();

            GameObject go = new GameObject("SelectableFloor");
            instantiated.Add(go);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();

            SelectionSubSystem system = new SelectionSubSystem();
            Selectable selectable = go.AddComponent<Selectable>();
            selectable.SelectionColor = system.RegisterSelectable(selectable);
            SelectionPickContext.RegisterSource(selectable);

            try
            {
                var draws = new List<SelectionPickContext.PickDraw>();
                SelectionPickContext.CollectPickDraws(draws);

                Assert.Greater(draws.Count, 0, "Expected at least one pick draw for the mesh");
                Assert.AreSame(mesh, draws[0].Mesh);
                Assert.IsNull(draws[0].SkinnedRenderer);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.IsTrue(block.isEmpty, "MeshRenderer must not keep a permanent selection MPB");
            }
            finally
            {
                SelectionPickContext.UnregisterSource(selectable);
            }

            Object.DestroyImmediate(mesh);
        }

        sealed class StubMeshResolver : IMeshAndDirectionResolver
        {
            readonly Mesh _mesh;

            public StubMeshResolver(Mesh mesh) => _mesh = mesh;

            public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
            {
                return new MeshDirectionInfo { Mesh = _mesh, Rotation = 0f };
            }
        }
    }
}
