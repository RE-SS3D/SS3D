using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Tile.MapEditor.Commands;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// What a sandbox player (a non-admin on an open-build server) may not build: anything that breaks
    /// the server's own loop. Spawn points keep respawn working; the nuclear device and disk would
    /// end the round (<c>sandbox-server.md</c> §5). Refused in the server RPCs and hidden from the palette.
    /// </summary>
    public static class MapEditorSandboxRules
    {
        /// <summary>Asset names (<see cref="GenericObjectSo.NameString"/>) of the nuclear device and its disk.</summary>
        private static readonly HashSet<string> DeniedAssets =
            new(StringComparer.OrdinalIgnoreCase) { "NukeCart", "NuclearAuthenticationDisk" };

        public const string DeniedMessage = "Spawn points and the nuclear device can't be built on this server.";

        /// <summary>Spawn placements and the denied assets; false for null or empty names.</summary>
        public static bool IsDeniedAsset(string assetName) =>
            !string.IsNullOrEmpty(assetName)
            && (MapEditorSpawnCatalog.IsSpawnKey(assetName) || DeniedAssets.Contains(assetName));

        /// <summary>
        /// Every spawn-point command, and anything else that names a denied asset, itself or anywhere in the contents
        /// its copied setup carries: a crate holding the disk is refused whole (object-setup-persistence Q15).
        /// </summary>
        public static bool IsDenied(MapEditorCommandDto command)
        {
            if (command.Kind is MapEditorCommandKind.PlaceSpawnPoint or MapEditorCommandKind.ClearSpawnPoint
                || IsDeniedAsset(command.AssetName))
                return true;

            if (command.Setup == null || command.Setup.Length == 0)
                return false;

            List<string> contained = new();
            ContainerContentsSetup.CollectItemNames(command.Setup, contained);
            foreach (string itemName in contained)
            {
                if (IsDeniedAsset(itemName))
                    return true;
            }

            return false;
        }

        /// <summary>Edit operations a command costs against the sandbox budget: one, plus one per item its copy carries.</summary>
        public static int OperationCost(MapEditorCommandDto command)
        {
            if (command.Setup == null || command.Setup.Length == 0)
                return 1;

            List<string> contained = new();
            ContainerContentsSetup.CollectItemNames(command.Setup, contained);
            return 1 + contained.Count;
        }
    }

    /// <summary>The server's per-connection answer to what the map editor UI should offer.</summary>
    [Serializable]
    public struct MapEditorCapabilities
    {
        /// <summary>Save, Export, Load, Delete and New Map.</summary>
        public bool Persistence;
        /// <summary>The Player Invincible setting.</summary>
        public bool Invincibility;
        /// <summary>Hide <see cref="MapEditorSandboxRules"/> denied entries from the palette.</summary>
        public bool RestrictedPalette;

        public static MapEditorCapabilities Full => For(persistenceAllowed: true, isSandboxPlayer: false);

        public static MapEditorCapabilities For(bool persistenceAllowed, bool isSandboxPlayer) => new()
        {
            Persistence = persistenceAllowed,
            Invincibility = MapEditorPermissions.MayToggleInvincibility(isSandboxPlayer),
            RestrictedPalette = isSandboxPlayer,
        };
    }
}
