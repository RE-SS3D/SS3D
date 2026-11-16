using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Builds machine panel location labels from the room the device sits in, so prefabs only
    /// author the device kind ("Atmospheric Vent") and the panel reads "Medbay — Atmospheric Vent".
    /// Server-side: resolves through the live <see cref="AreaSubSystem"/> registry.
    /// </summary>
    public static class MachineAreaLabel
    {
        public const string Separator = " — ";

        /// <summary>
        /// Display name of the room <paramref name="device"/> belongs to (wall mounts use the tile
        /// in front). False when the device has no tile object, no area, or the room is unnamed.
        /// </summary>
        public static bool TryGetAreaName(Component device, out string areaName)
        {
            areaName = null;
            if (device == null
                || !device.TryGetComponent(out PlacedTileObject tileObject)
                || !SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                || !areaSubSystem.TryGetAreaForDevice(tileObject, out AreaRecord record)
                || string.IsNullOrWhiteSpace(record.DisplayName))
            {
                return false;
            }

            areaName = record.DisplayName.Trim();
            return true;
        }

        /// <summary>The device's room name, or null when it is outside any named room.</summary>
        public static string GetAreaNameOrNull(Component device) =>
            TryGetAreaName(device, out string areaName) ? areaName : null;

        /// <summary>"{area} — {deviceKind}", or just one side when the other is empty.</summary>
        public static string Compose(string areaName, string deviceKind)
        {
            bool hasArea = !string.IsNullOrWhiteSpace(areaName);
            bool hasKind = !string.IsNullOrWhiteSpace(deviceKind);
            if (hasArea && hasKind)
            {
                return areaName.Trim() + Separator + deviceKind.Trim();
            }

            return hasArea ? areaName.Trim() : deviceKind?.Trim() ?? string.Empty;
        }

        /// <summary>Panel subtitle for <paramref name="device"/>: its room name followed by its kind.</summary>
        public static string ComposeForDevice(Component device, string deviceKind) =>
            Compose(GetAreaNameOrNull(device), deviceKind);
    }
}
