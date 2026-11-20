using Coimbra;
using EditorTests;
using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Floor paint: packing, square addressing, strokes, the editor command and save data.</summary>
    public sealed class FloorPaintTests
    {
        private readonly List<GameObject> _instantiated = new();
        private readonly Dictionary<string, GenericObjectSo> _assets = new();
        private static readonly Vector3 Cell = new(40, 0, 40);

        [TearDown]
        public void TearDown()
        {
            for (int i = _instantiated.Count - 1; i >= 0; i--)
            {
                if (_instantiated[i] != null)
                    Object.DestroyImmediate(_instantiated[i]);
            }

            _instantiated.Clear();
            _assets.Clear();
            FloorPaintView.Clear();
        }

        [Test]
        public void Apply_SetsOnlyTheMaskedSquares()
        {
            ushort paint = FloorPaint.Apply(0, 0b0101, 3);
            Assert.AreEqual(3, FloorPaint.Get(paint, 0));
            Assert.AreEqual(0, FloorPaint.Get(paint, 1));
            Assert.AreEqual(3, FloorPaint.Get(paint, 2));
            Assert.AreEqual(0, FloorPaint.Get(paint, 3));

            paint = FloorPaint.Apply(paint, 0b0011, 7);
            Assert.AreEqual(new[] { 7, 7, 3, 0 }, Enumerable.Range(0, 4).Select(square => FloorPaint.Get(paint, square)));

            Assert.AreEqual(0, FloorPaint.Apply(paint, 0b1111, 0), "colour 0 erases");
        }

        // A square is half a tile; tile (x, z) is centred on (x, z), so it spans x - 0.5 .. x + 0.5.
        [TestCase(0.2f, -0.3f, 0, 0, 1)]   // tile (0, 0), south-east
        [TestCase(-0.2f, 0.3f, 0, 0, 2)]   // tile (0, 0), north-west
        [TestCase(-0.8f, -1.2f, -1, -1, 1)] // tile (-1, -1), south-east: negative tiles floor, not truncate
        [TestCase(5.4f, 7.1f, 5, 7, 3)]    // tile (5, 7), north-east
        public void TexelAt_FindsTheTileAndSquare(float x, float z, int tileX, int tileZ, int square)
        {
            Vector2Int texel = FloorPaint.TexelAt(new Vector3(x, 0f, z));
            Assert.AreEqual(new Vector2Int(tileX, tileZ), FloorPaint.TileOf(texel));
            Assert.AreEqual(square, FloorPaint.SquareOf(texel));
            Assert.AreEqual(texel, FloorPaint.TexelOf(new Vector2Int(tileX, tileZ), square));
        }

        [Test]
        public void Stroke_LinesRectanglesAndTheCap()
        {
            var texels = new List<Vector2Int>();
            FloorPaint.Stroke(new Vector2Int(0, 0), new Vector2Int(3, 0), false, 100, texels);
            CollectionAssert.AreEqual(Enumerable.Range(0, 4).Select(x => new Vector2Int(x, 0)), texels);

            FloorPaint.Stroke(new Vector2Int(2, 2), new Vector2Int(0, 0), true, 100, texels);
            Assert.AreEqual(9, texels.Count);

            FloorPaint.Stroke(new Vector2Int(0, 0), new Vector2Int(10, 10), true, 100, texels);
            Assert.AreEqual(0, texels.Count, "121 squares is over the cap of 100");
        }

        [Test]
        public void MasksByTile_GroupsSquaresByTile()
        {
            Dictionary<Vector2Int, int> masks = FloorPaint.MasksByTile(new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 0),
            });

            Assert.AreEqual(0b1011, masks[new Vector2Int(0, 0)], "SW, SE and NE of tile (0, 0)");
            Assert.AreEqual(0b0001, masks[new Vector2Int(1, 0)], "SW of tile (1, 0)");
        }

        [Test]
        public void Palette_StartsWithTheDepartments_AndFitsFourBits()
        {
            Assert.LessOrEqual(FloorPaint.Palette.Count - 1, FloorPaint.MaxColors);
            CollectionAssert.AreEqual(
                new[] { "Command", "Security", "Engineering", "Medical", "Science", "Cargo", "Service", "Civilian" },
                FloorPaint.Palette.Skip(1).Select(swatch => swatch.Name));
            Assert.IsFalse(FloorPaint.IsPaint(0));
            Assert.IsFalse(FloorPaint.IsPaint(FloorPaint.ErasePreview));
        }

        [Test]
        public void Palette_TakesItsColoursFromDepartmentColors()
        {
            Assert.AreEqual(new Color32(0, 0, 0, 0), FloorPaint.ColorOf(0), "unpainted shows the floor");
            for (int index = 1; index < FloorPaint.Palette.Count; index++)
            {
                Department department = FloorPaint.Palette[index].Department;
                Assert.AreEqual((Color32)DepartmentColors.Get(department), FloorPaint.ColorOf(index), department.ToString());
                Assert.AreEqual(255, FloorPaint.ColorOf(index).a, $"{department} is opaque");
            }
        }

        [Test]
        public void DepartmentColors_LoadFromTheProjectSettingsAsset()
        {
            Assert.IsNotNull(ScriptableSettings.GetOrFind<DepartmentColors>(),
                "Assets/Settings/DepartmentColorsSettings.asset, preloaded, is where the colours are edited");
        }

        [Test]
        public void Command_PaintsAFloor_AndUndoPutsItBack()
        {
            TileMapTestUtilities.MapContext context = FloorAt(Cell);
            MapEditorCommandService service = CreateService(context);

            var commands = new List<IMapEditorCommand>();
            Assert.IsTrue(MapEditorCommandFactory.TryCreatePlaceCommands(PaintDto(0b0110, 4), service.Context, commands));
            Assert.IsTrue(service.ExecuteCompound(commands));
            Assert.IsTrue(context.Map.TryGetFloorPaint(Cell, out ushort paint));
            Assert.AreEqual(new[] { 0, 4, 4, 0 }, Enumerable.Range(0, 4).Select(square => FloorPaint.Get(paint, square)));

            Assert.IsTrue(service.Undo());
            context.Map.TryGetFloorPaint(Cell, out paint);
            Assert.AreEqual(0, paint);
        }

        [Test]
        public void Command_IsRefusedOffAFloor_AndForAColourThatDoesNotExist()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, Cell);
            TileMapTestUtilities.PlaceAirtightWall(context, Cell);
            MapEditorCommandService service = CreateService(context);
            var commands = new List<IMapEditorCommand>();

            Assert.IsFalse(MapEditorCommandFactory.TryCreatePlaceCommands(PaintDto(0b1111, 2), service.Context, commands), "a wall");
            Assert.IsFalse(MapEditorCommandFactory.TryCreatePlaceCommands(PaintDto(0b1111, 13), service.Context, commands), "no colour 13");
            Assert.IsEmpty(commands);
        }

        [Test]
        public void Command_PaintsTheFloorUnderAnAirlock()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, Cell);
            TileObjectSo airlock = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "PaintedAirlock");
            airlock.genericType = TileObjectGenericType.Door;
            Assert.IsTrue(context.Construction.TryPlaceTile(airlock, Cell, Direction.North, replaceExisting: false).Success);
            MapEditorCommandService service = CreateService(context);

            var commands = new List<IMapEditorCommand>();
            Assert.IsTrue(MapEditorCommandFactory.TryCreatePlaceCommands(PaintDto(0b1111, 3), service.Context, commands),
                "an airlock's floor halves show paint like a floor");
            Assert.IsTrue(service.ExecuteCompound(commands));
            context.Map.TryGetFloorPaint(Cell, out ushort paint);
            Assert.AreEqual(3, FloorPaint.Get(paint, 2));
        }

        [Test]
        public void RepaintingTheSameColour_IsNoUndoStep()
        {
            TileMapTestUtilities.MapContext context = FloorAt(Cell);
            MapEditorCommandService service = CreateService(context);
            Assert.IsTrue(service.Execute(new SetFloorPaintCommand(Cell, 0b1111, 2)));

            Assert.IsFalse(service.Execute(new SetFloorPaintCommand(Cell, 0b1111, 2)));
            Assert.AreEqual(1, service.UndoDepth);
        }

        [Test]
        public void ChunkSave_CarriesThePaint_AndLoadRestoresIt()
        {
            TileChunk painted = TileChunk.Create(Vector2Int.zero, Vector3.zero);
            _instantiated.Add(painted.gameObject);
            Assert.IsNull(painted.Save().floorPaint, "an unpainted chunk saves no paint");

            painted.SetFloorPaint(3, 5, FloorPaint.Apply(0, 0b1000, 6));
            SavedTileChunk saved = painted.Save();

            TileChunk loaded = TileChunk.Create(Vector2Int.zero, Vector3.zero);
            _instantiated.Add(loaded.gameObject);
            loaded.SetFloorPaint(saved.floorPaint);
            Assert.AreEqual(6, FloorPaint.Get(loaded.GetFloorPaint(3, 5), 3));
            Assert.AreEqual(0, loaded.GetFloorPaint(4, 5));
        }

        [Test]
        public void Catalog_ListsASwatchPerColour_UnderFloorPaint()
        {
            Assert.IsTrue(MapEditorFloorPaintCatalog.TryDecode(MapEditorFloorPaintCatalog.Encode(4), out int index));
            Assert.AreEqual(4, index);
            Assert.IsFalse(MapEditorFloorPaintCatalog.TryDecode(MapEditorFloorPaintCatalog.Encode(0), out _), "unpainted is no swatch");
            Assert.IsTrue(MapEditorCatalog.IsVirtualKey(MapEditorFloorPaintCatalog.Encode(1)));
            Assert.AreEqual("Medical", MapEditorCatalog.DisplayLabel(new MapEditorCatalogEntry
            {
                AssetName = MapEditorFloorPaintCatalog.Encode(4),
            }));
        }

        [Test]
        public void View_ReadsBackTheSyncedPaint_AndPublishesItsRect()
        {
            var paint = new ushort[TileChunk.ChunkSize * TileChunk.ChunkSize];
            paint[2 * TileChunk.ChunkSize + 1] = FloorPaint.Apply(0, 0b0001, 5);  // tile (-15, -14) of chunk (-1, -1)
            FloorPaintView.ReplaceChunks(new[] { (new Vector2Int(-1, -1), paint) });

            Vector2Int tile = new(-TileChunk.ChunkSize + 1, -TileChunk.ChunkSize + 2);
            Assert.AreEqual(5, FloorPaintView.IndexAt(FloorPaint.TexelOf(tile, 0)));
            Assert.AreEqual(0, FloorPaintView.IndexAt(FloorPaint.TexelOf(tile, 1)));
            Assert.Greater(Shader.GetGlobalVector("_FloorPaintRect").z, 0f, "the floor shader is told where the paint is");

            FloorPaintView.Clear();
            Assert.AreEqual(0f, Shader.GetGlobalVector("_FloorPaintRect").z, "and that there is none once cleared");
        }

        private TileMapTestUtilities.MapContext FloorAt(Vector3 cell)
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, cell);
            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "PaintedFloor");
            floor.genericType = TileObjectGenericType.Floor;
            Assert.IsTrue(context.Construction.TryPlaceTile(floor, cell, Direction.North, replaceExisting: false).Success);
            return context;
        }

        private static MapEditorCommandDto PaintDto(int mask, int color) => new()
        {
            Kind = MapEditorCommandKind.SetFloorPaint,
            Position = Cell,
            PaintMask = (byte)mask,
            PaintColor = (byte)color,
        };

        private MapEditorCommandService CreateService(TileMapTestUtilities.MapContext context)
        {
            var service = new MapEditorCommandService();
            service.Bind(context.Map, loader: null, context.Construction);
            service.Context.AssetResolver = name => _assets.TryGetValue(name, out GenericObjectSo asset) ? asset : null;
            return service;
        }
    }
}
