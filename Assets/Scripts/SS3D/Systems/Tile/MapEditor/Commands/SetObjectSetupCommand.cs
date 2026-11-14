using SS3D.Data.AssetDatabases;
using SS3D.Data.Persistence;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    /// <summary>
    /// An object's setup changed in-world (a valve turned, a locker filled) while its user had the editor open
    /// (Documents/architecture/2026-11_object-setup-persistence.md Q14). The change has already happened when the
    /// command is recorded, so <see cref="Apply"/> only runs on redo. Finds its object again by the tile slot or the
    /// item it was captured from, so it survives the object being re-spawned by other undo steps.
    /// </summary>
    public sealed class SetObjectSetupCommand : IMapEditorCommand, IReportsChange
    {
        private readonly string _assetName;
        private readonly Vector3 _position;
        private readonly Direction _direction;
        private readonly SavedSetupEntry[] _before;
        private readonly SavedSetupEntry[] _after;

        public SetObjectSetupCommand(string assetName, Vector3 position, Direction direction,
            SavedSetupEntry[] before, SavedSetupEntry[] after)
        {
            _assetName = assetName;
            _position = position;
            _direction = direction;
            _before = before;
            _after = after;
        }

        /// <summary>A command for <paramref name="placedRoot"/>, a placed tile object or a placed item.</summary>
        public static bool TryCreate(GameObject placedRoot, SavedSetupEntry[] before, SavedSetupEntry[] after,
            out SetObjectSetupCommand command)
        {
            command = null;
            if (placedRoot == null || ObjectSetup.SameSetup(before, after))
                return false;

            if (placedRoot.TryGetComponent(out PlacedTileObject tile) && tile.tileObjectSO != null)
            {
                command = new SetObjectSetupCommand(tile.NameString,
                    new Vector3(tile.WorldOrigin.x, 0f, tile.WorldOrigin.y), tile.Direction, before, after);
                return true;
            }

            if (placedRoot.TryGetComponent(out PlacedItemObject item))
            {
                command = new SetObjectSetupCommand(item.NameString, item.transform.position, Direction.North, before, after);
                return true;
            }

            return false;
        }

        public bool Changed => !ObjectSetup.SameSetup(_before, _after);

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.SetObjectSetup,
                AssetName = _assetName,
                Position = _position,
                Direction = _direction,
                Setup = _after,
                PreviousSetup = _before,
            };

        public void Apply(MapEditorCommandContext ctx) => Set(ctx, _after);

        public void Revert(MapEditorCommandContext ctx) => Set(ctx, _before);

        private void Set(MapEditorCommandContext ctx, SavedSetupEntry[] setup)
        {
            GameObject target = FindTarget(ctx);
            if (target != null)
                ObjectSetup.Restore(target, setup, _assetName, resetUnlisted: true);
        }

        private GameObject FindTarget(MapEditorCommandContext ctx)
        {
            if (ctx.Map == null)
                return null;

            GenericObjectSo asset = ctx.ResolveAsset(_assetName);
            if (asset is TileObjectSo tile)
            {
                return ctx.Map.TryGetTileLocation(tile.layer, _position, out ITileLocation location)
                    && location.TryGetPlacedObject(out PlacedTileObject placed, _direction)
                    && placed != null && placed.tileObjectSO == tile
                        ? placed.gameObject
                        : null;
            }

            if (asset is ItemObjectSo item)
            {
                PlacedItemObject placed = ctx.Map.FindItemObject(_position, item);
                return placed != null ? placed.gameObject : null;
            }

            return null;
        }
    }
}
