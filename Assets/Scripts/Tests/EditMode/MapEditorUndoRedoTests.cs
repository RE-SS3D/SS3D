using EditorTests;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.Commands;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class MapEditorUndoRedoTests
    {
        private readonly List<GameObject> _instantiated = new();
        private readonly Dictionary<string, GenericObjectSo> _assets = new();

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
        }

        [Test]
        public void Place_Undo_ClearsTile_Redo_Restores()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(3, 0, 3);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);

            TileObjectSo floor = RegisterTile(TileLayer.Turf, "UndoFloor", TileObjectGenericType.Floor);
            MapEditorCommandService service = CreateService(mapCtx);

            Assert.IsTrue(service.Execute(new PlaceTileCommand(floor.NameString, pos, Direction.North, false)));
            Assert.AreEqual(1, service.UndoDepth);
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.Turf, pos));

            Assert.IsTrue(service.Undo());
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, pos));
            Assert.AreEqual(1, service.RedoDepth);

            Assert.IsTrue(service.Redo());
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.Turf, pos));
        }

        [Test]
        public void Replace_Undo_RestoresPreviousAsset()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(4, 0, 4);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);

            TileObjectSo first = RegisterTile(TileLayer.Turf, "FirstFloor", TileObjectGenericType.Floor);
            TileObjectSo second = RegisterTile(TileLayer.Turf, "SecondFloor", TileObjectGenericType.Floor);
            MapEditorCommandService service = CreateService(mapCtx);

            Assert.IsTrue(service.Execute(new PlaceTileCommand(first.NameString, pos, Direction.North, false)));

            MapEditorCommandDto replaceDto = new()
            {
                Kind = MapEditorCommandKind.PlaceTile,
                AssetName = second.NameString,
                Position = pos,
                Direction = Direction.North,
                ReplaceExisting = true,
            };
            IMapEditorCommand replace = MapEditorCommandFactory.FromDto(replaceDto, service.Context);
            Assert.IsTrue(service.Execute(replace));

            Assert.AreEqual("SecondFloor", GetOccupantName(mapCtx, TileLayer.Turf, pos));

            Assert.IsTrue(service.Undo());
            Assert.AreEqual("FirstFloor", GetOccupantName(mapCtx, TileLayer.Turf, pos));
        }

        [Test]
        public void CompoundPlace_OneUndo_ClearsAll()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            TileObjectSo floor = RegisterTile(TileLayer.Turf, "BatchFloor", TileObjectGenericType.Floor);
            MapEditorCommandService service = CreateService(mapCtx);

            Vector3 a = new(1, 0, 1);
            Vector3 b = new(2, 0, 1);
            Vector3 c = new(3, 0, 1);
            TileMapTestUtilities.PlacePlenum(mapCtx, a);
            TileMapTestUtilities.PlacePlenum(mapCtx, b);
            TileMapTestUtilities.PlacePlenum(mapCtx, c);

            Assert.IsTrue(service.ExecuteCompound(new IMapEditorCommand[]
            {
                new PlaceTileCommand(floor.NameString, a, Direction.North, false),
                new PlaceTileCommand(floor.NameString, b, Direction.North, false),
                new PlaceTileCommand(floor.NameString, c, Direction.North, false),
            }));

            Assert.AreEqual(1, service.UndoDepth);
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.Turf, a));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.Turf, b));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.Turf, c));

            Assert.IsTrue(service.Undo());
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, a));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, b));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, c));
        }

        [Test]
        public void FloorDecal_Undo_Clears_Redo_Restores()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(5, 0, 5);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);
            MapEditorCommandService service = CreateService(mapCtx);

            MapEditorCommandDto dto = new()
            {
                Kind = MapEditorCommandKind.SetFloorDecal,
                Position = pos,
                DecalId = 7,
            };
            Assert.IsTrue(service.Execute(MapEditorCommandFactory.FromDto(dto, service.Context)));

            Assert.IsTrue(mapCtx.Map.TryGetFloorDecalId(pos, out ushort id));
            Assert.AreEqual(7, id);

            Assert.IsTrue(service.Undo());
            Assert.IsTrue(mapCtx.Map.TryGetFloorDecalId(pos, out id));
            Assert.AreEqual(0, id);

            Assert.IsTrue(service.Redo());
            Assert.IsTrue(mapCtx.Map.TryGetFloorDecalId(pos, out id));
            Assert.AreEqual(7, id);
        }

        [Test]
        public void Clear_Undo_ReplacesClearedAsset()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(6, 0, 6);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);

            TileObjectSo floor = RegisterTile(TileLayer.Turf, "ClearableFloor", TileObjectGenericType.Floor);
            MapEditorCommandService service = CreateService(mapCtx);

            Assert.IsTrue(service.Execute(new PlaceTileCommand(floor.NameString, pos, Direction.North, false)));
            Assert.IsTrue(service.Execute(new ClearTileCommand(floor.NameString, pos, Direction.North)));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, pos));

            Assert.IsTrue(service.Undo());
            Assert.AreEqual("ClearableFloor", GetOccupantName(mapCtx, TileLayer.Turf, pos));
        }

        [Test]
        public void ReplaceWall_KeepsWallMounts_AndUndoKeepsThem()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(7, 0, 7);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);

            TileObjectSo steel = RegisterTile(TileLayer.Turf, "SteelWallUndo", TileObjectGenericType.Wall);
            TileObjectSo reinforced = RegisterTile(TileLayer.Turf, "ReinforcedWallUndo", TileObjectGenericType.Wall);
            TileObjectSo mount = RegisterTile(TileLayer.WallMountHigh, "WallMountUndo", TileObjectGenericType.None);
            MapEditorCommandService service = CreateService(mapCtx);

            Assert.IsTrue(service.Execute(new PlaceTileCommand(steel.NameString, pos, Direction.North, false)));
            Assert.IsTrue(service.Execute(new PlaceTileCommand(mount.NameString, pos, Direction.North, false)));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos), "mount should build on the wall");

            Assert.IsTrue(service.Execute(Replace(service, reinforced, pos)));
            Assert.AreEqual("ReinforcedWallUndo", GetOccupantName(mapCtx, TileLayer.Turf, pos));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos), "a wall-for-wall swap keeps its mounts");

            Assert.IsTrue(service.Undo());
            Assert.AreEqual("SteelWallUndo", GetOccupantName(mapCtx, TileLayer.Turf, pos));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos), "undoing the swap keeps the mounts");

            Assert.IsTrue(service.Redo());
            Assert.AreEqual("ReinforcedWallUndo", GetOccupantName(mapCtx, TileLayer.Turf, pos));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos));
        }

        [Test]
        public void ReplaceWallWithFloor_DropsMounts_UndoRestoresThem()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(8, 0, 8);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);

            TileObjectSo wall = RegisterTile(TileLayer.Turf, "WallToFloor", TileObjectGenericType.Wall);
            TileObjectSo floor = RegisterTile(TileLayer.Turf, "FloorFromWall", TileObjectGenericType.Floor);
            TileObjectSo mount = RegisterTile(TileLayer.WallMountLow, "LowMountUndo", TileObjectGenericType.None);
            MapEditorCommandService service = CreateService(mapCtx);

            Assert.IsTrue(service.Execute(new PlaceTileCommand(wall.NameString, pos, Direction.North, false)));
            Assert.IsTrue(service.Execute(new PlaceTileCommand(mount.NameString, pos, Direction.East, false)));

            Assert.IsTrue(service.Execute(Replace(service, floor, pos)));
            Assert.AreEqual("FloorFromWall", GetOccupantName(mapCtx, TileLayer.Turf, pos));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.WallMountLow, pos, Direction.East), "a floor holds no mounts");

            Assert.IsTrue(service.Undo());
            Assert.AreEqual("WallToFloor", GetOccupantName(mapCtx, TileLayer.Turf, pos));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.WallMountLow, pos, Direction.East), "undo brings the mount back");
        }

        [Test]
        public void ClearWall_Undo_RestoresWallMounts_Redo_ClearsThemAgain()
        {
            TileMapTestUtilities.MapContext mapCtx = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(9, 0, 9);
            TileMapTestUtilities.PlacePlenum(mapCtx, pos);

            TileObjectSo wall = RegisterTile(TileLayer.Turf, "ClearedWall", TileObjectGenericType.Wall);
            TileObjectSo mount = RegisterTile(TileLayer.WallMountHigh, "ClearedWallMount", TileObjectGenericType.None);
            MapEditorCommandService service = CreateService(mapCtx);

            Assert.IsTrue(service.Execute(new PlaceTileCommand(wall.NameString, pos, Direction.North, false)));
            Assert.IsTrue(service.Execute(new PlaceTileCommand(mount.NameString, pos, Direction.South, false)));

            Assert.IsTrue(service.Execute(new ClearTileCommand(wall.NameString, pos, Direction.North)));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, pos));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos, Direction.South), "clearing a wall drops its mounts");

            Assert.IsTrue(service.Undo());
            Assert.AreEqual("ClearedWall", GetOccupantName(mapCtx, TileLayer.Turf, pos));
            Assert.IsTrue(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos, Direction.South), "undo brings the mount back");

            Assert.IsTrue(service.Redo());
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.Turf, pos));
            Assert.IsFalse(HasOccupant(mapCtx, TileLayer.WallMountHigh, pos, Direction.South));
        }

        private static IMapEditorCommand Replace(MapEditorCommandService service, TileObjectSo tile, Vector3 pos) =>
            MapEditorCommandFactory.FromDto(new MapEditorCommandDto
            {
                Kind = MapEditorCommandKind.PlaceTile,
                AssetName = tile.NameString,
                Position = pos,
                Direction = Direction.North,
                ReplaceExisting = true,
            }, service.Context);

        private MapEditorCommandService CreateService(TileMapTestUtilities.MapContext mapCtx)
        {
            var service = new MapEditorCommandService();
            service.Bind(mapCtx.Map, loader: null, mapCtx.Construction);
            service.Context.AssetResolver = name =>
                _assets.TryGetValue(name, out GenericObjectSo asset) ? asset : null;
            return service;
        }

        private TileObjectSo RegisterTile(TileLayer layer, string name, TileObjectGenericType genericType)
        {
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(layer, name);
            so.genericType = genericType;
            _assets[so.NameString] = so;
            return so;
        }

        private static bool HasOccupant(TileMapTestUtilities.MapContext ctx, TileLayer layer, Vector3 pos,
            Direction direction = Direction.North) =>
            ctx.Map.TryGetTileLocation(layer, pos, out ITileLocation location)
            && location.TryGetPlacedObject(out PlacedTileObject placed, direction)
            && placed != null;

        private static string GetOccupantName(TileMapTestUtilities.MapContext ctx, TileLayer layer, Vector3 pos)
        {
            Assert.IsTrue(ctx.Map.TryGetTileLocation(layer, pos, out ITileLocation location));
            Assert.IsTrue(location.TryGetPlacedObject(out PlacedTileObject placed));
            return placed.NameString;
        }
    }
}
