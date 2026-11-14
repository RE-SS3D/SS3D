using SS3D.Data.AssetDatabases;
using SS3D.Data.Persistence;
using SS3D.Systems.Tile.MapEditor.Commands;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// The Dropper's copy of an object's setup (Documents/architecture/2026-11_object-setup-persistence.md Q5), held on
    /// the client alongside the palette selection, and the size cap on setup a submit may carry. The setup itself lives
    /// on the server, which sends it back after a Dropper click.
    /// </summary>
    public sealed class MapEditorSetupCopy
    {
        /// <summary>
        /// Copied setup JSON one submit may carry. FishNet splits big messages, but a filled crate pasted down a drag
        /// line multiplies; a paste over the cap is refused, never truncated.
        /// </summary>
        public const int MaxCharsPerSubmit = 64 * 1024;

        public const string TooLargeMessage = "Too much to paste with its contents at once. Place fewer copies.";

        private SavedSetupEntry[] _setup;
        private string _assetName;

        /// <summary>The server sent the setup of the object the Dropper copied.</summary>
        public void Hold(string assetName, SavedSetupEntry[] setup)
        {
            _assetName = assetName;
            _setup = setup;
        }

        /// <summary>A palette pick, a new Dropper click or closing the editor: places give the default object again.</summary>
        public void Clear()
        {
            _assetName = null;
            _setup = null;
        }

        /// <summary>Gives each place of the copied asset that has no setup of its own the copied setup.</summary>
        public void ApplyTo(MapEditorCommandDto[] commands)
        {
            if (_setup == null)
                return;

            for (int i = 0; i < commands.Length; i++)
            {
                if (commands[i].Kind is MapEditorCommandKind.PlaceTile or MapEditorCommandKind.PlaceItem
                    && commands[i].Setup == null && commands[i].AssetName == _assetName)
                {
                    commands[i].Setup = _setup;
                }
            }
        }

        /// <summary>Total setup JSON characters, nested contents included (each level is a string in its parent's).</summary>
        public static int Chars(IReadOnlyList<SavedSetupEntry> setup)
        {
            int total = 0;
            if (setup == null)
                return 0;

            foreach (SavedSetupEntry entry in setup)
                total += (entry.key?.Length ?? 0) + (entry.json?.Length ?? 0);

            return total;
        }

        public static int Chars(IEnumerable<MapEditorCommandDto> commands)
        {
            int total = 0;
            foreach (MapEditorCommandDto dto in commands)
                total += Chars(dto.Setup) + Chars(dto.PreviousSetup);

            return total;
        }

        /// <summary>Server: the placed object a Dropper click named, or null when it's gone.</summary>
        public static GameObject FindPlaced(TileMap map, GenericObjectSo asset, bool isItem, Vector3 position, Direction direction)
        {
            if (map == null)
                return null;

            if (isItem)
                return asset is ItemObjectSo item && map.FindItemObject(position, item) is { } placedItem ? placedItem.gameObject : null;

            return asset is TileObjectSo tile
                && map.TryGetTileLocation(tile.layer, position, out ITileLocation location)
                && location.TryGetPlacedObject(out PlacedTileObject placed, direction)
                && placed != null && placed.tileObjectSO == tile
                    ? placed.gameObject
                    : null;
        }
    }
}
