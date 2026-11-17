using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// The tiles a blast reached, packed for the detonation RPC so every client can show flames over the blast's real
    /// shape. Three bytes a tile: the x and y offset from the epicenter (signed) and the force there as a share of the
    /// strongest tile (0-255). Pure, EditMode-tested.
    /// </summary>
    public static class BlastFlameTiles
    {
        public const int BytesPerTile = 3;

        /// <summary>
        /// A bigger blast keeps its strongest tiles (3 KB at most). A Syndicate bomb in open space reaches about 840, so
        /// its faint outer ring goes unlit.
        /// </summary>
        public const int MaxTiles = 1000;

        /// <summary>Seconds the flame front takes to cross one tile, so the fire spreads out from the epicenter.</summary>
        public const float HopSeconds = 0.035f;

        public readonly struct Flame
        {
            public readonly Vector2Int Offset;
            public readonly float Strength;

            public Flame(Vector2Int offset, float strength)
            {
                Offset = offset;
                Strength = strength;
            }

            /// <summary>
            /// When this tile catches, after the blast: one hop per tile of straight-line distance, so the front
            /// grows as a circle like the blast's own spread.
            /// </summary>
            public float Delay => Offset.magnitude * HopSeconds;
        }

        public static byte[] Pack(TileCoord epicenter, IReadOnlyDictionary<TileCoord, float> forces)
        {
            if (forces == null || forces.Count == 0)
                return Array.Empty<byte>();

            List<KeyValuePair<TileCoord, float>> tiles = forces
                .Where(entry => entry.Key.MapId == epicenter.MapId && entry.Value >= BlastResolutionService.MinUsefulForce
                    && InRange(entry.Key.Grid.x - epicenter.Grid.x) && InRange(entry.Key.Grid.y - epicenter.Grid.y))
                .OrderByDescending(entry => entry.Value)
                .Take(MaxTiles)
                .ToList();
            if (tiles.Count == 0)
                return Array.Empty<byte>();

            float strongest = tiles[0].Value;
            byte[] packed = new byte[tiles.Count * BytesPerTile];
            for (int i = 0; i < tiles.Count; i++)
            {
                packed[i * BytesPerTile] = unchecked((byte)(sbyte)(tiles[i].Key.Grid.x - epicenter.Grid.x));
                packed[i * BytesPerTile + 1] = unchecked((byte)(sbyte)(tiles[i].Key.Grid.y - epicenter.Grid.y));
                packed[i * BytesPerTile + 2] = (byte)Mathf.Clamp(Mathf.RoundToInt(tiles[i].Value / strongest * 255f), 1, 255);
            }

            return packed;
        }

        public static List<Flame> Unpack(byte[] packed)
        {
            List<Flame> flames = new();
            if (packed == null)
                return flames;

            for (int i = 0; i + BytesPerTile <= packed.Length; i += BytesPerTile)
            {
                Vector2Int offset = new(unchecked((sbyte)packed[i]), unchecked((sbyte)packed[i + 1]));
                flames.Add(new Flame(offset, packed[i + 2] / 255f));
            }

            return flames;
        }

        private static bool InRange(int offset) => offset >= sbyte.MinValue && offset <= sbyte.MaxValue;
    }
}
