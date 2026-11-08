using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class StructuralIntegrityPresentationTests
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
        public void ResolveTint_DarkensDamagedAndCracked()
        {
            Color intact = StructuralIntegrityPresenter.ResolveTint(StructuralIntegrityStage.Intact);
            Color damaged = StructuralIntegrityPresenter.ResolveTint(StructuralIntegrityStage.Damaged);
            Color cracked = StructuralIntegrityPresenter.ResolveTint(StructuralIntegrityStage.Cracked);

            Assert.AreEqual(Color.white, intact);
            Assert.Less(damaged.r + damaged.g + damaged.b, intact.r + intact.g + intact.b);
            Assert.Less(cracked.r + cracked.g + cracked.b, damaged.r + damaged.g + damaged.b);
        }

        [Test]
        public void Presenter_Apply_SetsAppliedStage()
        {
            var go = new GameObject("WallPresenter");
            _instantiated.Add(go);
            go.AddComponent<MeshRenderer>();
            StructuralIntegrityPresenter presenter = go.AddComponent<StructuralIntegrityPresenter>();

            presenter.Apply(StructuralIntegrityStage.Cracked);
            Assert.AreEqual(StructuralIntegrityStage.Cracked, presenter.AppliedStage);

            presenter.Apply(StructuralIntegrityStage.Intact);
            Assert.AreEqual(StructuralIntegrityStage.Intact, presenter.AppliedStage);
        }

        [Test]
        public void Presenter_ApplyIntact_ClearsMaterialPropertyBlock()
        {
            var go = new GameObject("WallPresenterMpb");
            _instantiated.Add(go);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            StructuralIntegrityPresenter presenter = go.AddComponent<StructuralIntegrityPresenter>();

            presenter.Apply(StructuralIntegrityStage.Damaged);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.IsFalse(block.isEmpty, "Damaged stage should write a tint MPB");

            presenter.Apply(StructuralIntegrityStage.Intact);
            renderer.GetPropertyBlock(block);
            Assert.IsTrue(block.isEmpty, "Intact should clear MPB for SRP Batcher / GPU Instancing");
        }

        [TestCase("SteelWall")]
        [TestCase("SteelWallReinforced")]
        [TestCase("SteelWindow")]
        [TestCase("SteelWindowReinforced")]
        [TestCase("SteelGirder")]
        public void WallVariants_ShowDamageOnVisibleSurfaces(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Content/WorldObjects/Structures/Walls/{prefabName}.prefab");
            var wall = Object.Instantiate(prefab);
            _instantiated.Add(wall);

            var presenter = wall.GetComponent<StructuralIntegrityPresenter>();
            presenter.Apply(StructuralIntegrityStage.Cracked);

            var overlays = wall.GetComponentsInChildren<MeshRenderer>();
            Assert.IsTrue(System.Array.Exists(overlays, renderer =>
                renderer.name == "Steel wall damage overlay" && !renderer.forceRenderingOff
                && renderer.sharedMaterial.GetFloat("_Cracked") == 1f));
        }

        [Test]
        public void SteelWallOverlay_FollowsAdjacencyMeshAndClearsWhenRepaired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Content/WorldObjects/Structures/Walls/SteelWall.prefab");
            var wall = Object.Instantiate(prefab);
            _instantiated.Add(wall);

            var presenter = wall.GetComponent<StructuralIntegrityPresenter>();
            presenter.Apply(StructuralIntegrityStage.Damaged);
            var overlay = wall.transform.Find("Steel wall damage overlay");
            Assert.IsNotNull(overlay);
            var overlayMesh = overlay.GetComponent<MeshFilter>();
            var initialMesh = overlayMesh.sharedMesh;

            // Awake does not configure the adjacency view in EditMode.
            var connector = wall.GetComponent<WallAdjacencyConnector>();
            var configure = typeof(EngineDrivenHorizontalConnector).GetMethod("ConfigureAdjacencyView",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            configure.Invoke(connector, null);
            byte straight = (byte)((1 << (int)Direction.East) | (1 << (int)Direction.West));
            connector.SetAdjacencyConnections(straight);

            Assert.AreNotSame(initialMesh, wall.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreSame(wall.GetComponent<MeshFilter>().sharedMesh, overlayMesh.sharedMesh);
            Assert.AreEqual(0f, overlay.GetComponent<MeshRenderer>().sharedMaterial.GetFloat("_Cracked"));

            presenter.Apply(StructuralIntegrityStage.Cracked);
            Assert.AreEqual(1f, overlay.GetComponent<MeshRenderer>().sharedMaterial.GetFloat("_Cracked"));

            presenter.Apply(StructuralIntegrityStage.Intact);
            Assert.IsFalse(overlay.gameObject.activeSelf);
        }

        [TestCase("Assets/Content/WorldObjects/Furniture/Generic/Surfaces/Tables/TableSteel.prefab", "Assets/Content/Data/TileMap/Resources/Furniture/Generic/Tables/TableSteel.asset", 1, 1f)]
        [TestCase("Assets/Content/WorldObjects/Structures/Doors/CivillianAirlock.prefab", "Assets/Content/Data/TileMap/Resources/Structures/Doors/AirlockCivilian.asset", 2, 2f)]
        [TestCase("Assets/Content/WorldObjects/Structures/Doors/SecurityAirlock.prefab", "Assets/Content/Data/TileMap/Resources/Structures/Doors/AirlockSecurity.asset", 2, 2f)]
        [TestCase("Assets/Content/WorldObjects/Furniture/Generic/Surfaces/Tables/TableWood.prefab", "Assets/Content/Data/TileMap/Resources/Furniture/Generic/Tables/TableWood.asset", 1, 1f)]
        [TestCase("Assets/Content/WorldObjects/Furniture/Generic/Surfaces/Tables/TableGlass.prefab", "Assets/Content/Data/TileMap/Resources/Furniture/Generic/Tables/TableGlass.asset", 1, 1f)]
        public void Objects_OverlayFollowsSurfaceAndClearsOnRepair(string prefabPath, string soPath, int count, float mode)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            _instantiated.Add(root);
            var placed = root.GetComponent<PlacedTileObject>();
            var serialized = new SerializedObject(placed);
            serialized.FindProperty("_tileObjectSo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileObjectSo>(soPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!root.TryGetComponent(out StructuralIntegrityPresenter presenter))
                presenter = root.AddComponent<StructuralIntegrityPresenter>();
            presenter.Apply(StructuralIntegrityStage.Damaged);
            var overlays = new List<MeshRenderer>();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                if (renderer.name == "Steel wall damage overlay") overlays.Add(renderer);
            Assert.AreEqual(count, overlays.Count, "Only the tabletop or moving leaves should be marked");
            foreach (var overlay in overlays)
            {
                var block = new MaterialPropertyBlock();
                overlay.GetPropertyBlock(block);
                Assert.AreEqual(mode, block.GetFloat("_SurfaceMode"));
                Assert.AreEqual(soPath.Contains("TableWood") ? 1f : 0f, block.GetFloat("_Wood"));
                Assert.AreEqual(soPath.Contains("TableGlass") ? 1f : 0f, block.GetFloat("_Glass"));
                Transform surface = overlay.transform.parent;
                Assert.AreSame(surface.GetComponent<MeshFilter>().sharedMesh, overlay.GetComponent<MeshFilter>().sharedMesh);
                Vector3 original = overlay.transform.position;
                surface.position += Vector3.right;
                Assert.AreEqual(original + Vector3.right, overlay.transform.position);
            }
            presenter.Apply(StructuralIntegrityStage.Cracked);
            foreach (var overlay in overlays)
                Assert.AreEqual(1f, overlay.sharedMaterial.GetFloat("_Cracked"));
            Assert.IsNull(root.GetComponent<AudioSource>(), "Metal objects must not play the hull leak hiss");
            presenter.Apply(StructuralIntegrityStage.Intact);
            foreach (var overlay in overlays)
                Assert.IsFalse(overlay.gameObject.activeSelf);
        }

        [Test]
        public void ExamineProvider_AppendsCrackedLine()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 wallPos = new Vector3(5, 0, 5);
            TileMapTestUtilities.PlacePlenum(context, wallPos);
            TileMapTestUtilities.PlaceAirtightWall(context, wallPos);

            Assert.IsTrue(context.Query.TryGetOccupant(
                context.Query.WorldToTile(wallPos), TileLayer.Turf, Direction.North, out ITileOccupant occupant));
            var placed = (PlacedTileObject)occupant;
            placed.ServerSetIntegrity(30f, StructuralIntegrityStage.Cracked);

            GameObject host = placed.gameObject;
            StructuralIntegrityExaminable examinable = host.AddComponent<StructuralIntegrityExaminable>();
            var sections = new List<ExamineSection>();
            examinable.AppendSections(examinable, sections);

            Assert.AreEqual(1, sections.Count);
            Assert.IsTrue(
                sections[0].Text.IndexOf("cracked", System.StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected cracked examine text, got: {sections[0].Text}");
        }
    }
}
