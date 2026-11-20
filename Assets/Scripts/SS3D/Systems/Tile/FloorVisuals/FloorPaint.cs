using SS3D.Systems.IdAccess;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.FloorVisuals
{
    /// <summary>
    /// Floor paint: every floor tile is drawn as 2 × 2 squares (<c>StationFloorTile.shader</c>), and each square can be
    /// painted one of the <see cref="Palette"/> colours. A tile's paint is one <see cref="ushort"/>: four 4-bit palette
    /// indices, one per square, 0 for unpainted.
    /// <para>
    /// Squares are addressed in half-tile <b>texels</b>: tile (x, z) covers texels (2x, 2z) to (2x + 1, 2z + 1), and the
    /// square under world point p is <c>floor((p.xz + 0.5) × 2)</c>, the same lookup the shader makes.
    /// </para>
    /// </summary>
    public static class FloorPaint
    {
        /// <summary>One paint colour: a department's. Saved maps store the index, so entries are only ever appended, never reordered.</summary>
        public readonly struct Swatch
        {
            public Swatch(Department department)
            {
                Department = department;
            }

            public Department Department { get; }

            public string Name => Department == Department.None ? "Unpainted" : DepartmentDisplay.GetName(Department);

            /// <summary>sRGB, from <see cref="DepartmentColors"/>; transparent for unpainted.</summary>
            public Color32 Color => Department == Department.None ? default : DepartmentColors.Get(Department);
        }

        /// <summary>
        /// The paint colours, index 1 up (index 0 is unpainted): the departments, coloured by <see cref="DepartmentColors"/>
        /// (Project Settings › SS3D › Assets › Department Colors). Append new entries at the end: saved maps hold these
        /// indices. At most <see cref="MaxColors"/>.
        /// </summary>
        public static readonly IReadOnlyList<Swatch> Palette = new Swatch[]
        {
            new(Department.None),
            new(Department.Command),
            new(Department.Security),
            new(Department.Engineering),
            new(Department.Medical),
            new(Department.Science),
            new(Department.Cargo),
            new(Department.Service),
            new(Department.Civilian),
        };

        /// <summary>Highest index a square can hold (4 bits; 15 is the editor's erase preview).</summary>
        public const int MaxColors = 14;

        /// <summary>Not a paint: the map editor shows squares about to be erased in this colour.</summary>
        public const byte ErasePreview = 15;

        public static readonly Color32 ErasePreviewColor = new(0xe0, 0x4a, 0x4a, 0xff);

        public static bool IsPaint(int index) => index > 0 && index < Palette.Count;

        public static Color32 ColorOf(int index) =>
            index == ErasePreview ? ErasePreviewColor : IsPaint(index) ? Palette[index].Color : default;

        public static string NameOf(int index) => IsPaint(index) ? Palette[index].Name : Palette[0].Name;

        /// <summary>Square index within a tile: 0 south-west, 1 south-east, 2 north-west, 3 north-east.</summary>
        public static int SquareOf(Vector2Int texel) => (texel.y & 1) * 2 + (texel.x & 1);

        /// <summary>The tile a texel belongs to (arithmetic shift: floor division, so negative tiles work too).</summary>
        public static Vector2Int TileOf(Vector2Int texel) => new(texel.x >> 1, texel.y >> 1);

        public static Vector2Int TexelOf(Vector2Int tile, int square) => new(tile.x * 2 + (square & 1), tile.y * 2 + (square >> 1));

        /// <summary>The square under a world point, as the shader picks it.</summary>
        public static Vector2Int TexelAt(Vector3 world) =>
            new(Mathf.FloorToInt((world.x + 0.5f) * 2f), Mathf.FloorToInt((world.z + 0.5f) * 2f));

        public static int Get(ushort paint, int square) => (paint >> (square * 4)) & 0xF;

        public static ushort Set(ushort paint, int square, int index) =>
            (ushort)((paint & ~(0xF << (square * 4))) | ((index & 0xF) << (square * 4)));

        /// <summary>
        /// <paramref name="paint"/> with every square in <paramref name="mask"/> (bit per square) set to
        /// <paramref name="index"/>; index 0 erases.
        /// </summary>
        public static ushort Apply(ushort paint, int mask, int index)
        {
            for (int square = 0; square < 4; square++)
            {
                if ((mask & (1 << square)) != 0)
                    paint = Set(paint, square, index);
            }

            return paint;
        }

        /// <summary>
        /// The squares a drag covers, from one texel to another: a line, or with <paramref name="rectangle"/> every
        /// square in the box. Empty when the box is over <paramref name="maxTexels"/>.
        /// </summary>
        public static void Stroke(Vector2Int start, Vector2Int end, bool rectangle, int maxTexels, List<Vector2Int> into)
        {
            into.Clear();
            int width = Math.Abs(end.x - start.x) + 1;
            int height = Math.Abs(end.y - start.y) + 1;
            if (rectangle)
            {
                if ((long)width * height > maxTexels)
                    return;

                int x0 = Math.Min(start.x, end.x);
                int y0 = Math.Min(start.y, end.y);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        into.Add(new Vector2Int(x0 + x, y0 + y));
                }

                return;
            }

            int steps = Math.Max(width, height);
            if (steps > maxTexels)
                return;

            for (int i = 0; i < steps; i++)
            {
                Vector2Int texel = Vector2Int.RoundToInt(Vector2.Lerp(start, end, steps == 1 ? 0f : (float)i / (steps - 1)));
                if (into.Count == 0 || into[into.Count - 1] != texel)
                    into.Add(texel);
            }
        }

        /// <summary>Groups squares by tile: each tile once, with a bit per square of it in the stroke.</summary>
        public static Dictionary<Vector2Int, int> MasksByTile(IEnumerable<Vector2Int> texels)
        {
            var masks = new Dictionary<Vector2Int, int>();
            foreach (Vector2Int texel in texels)
            {
                Vector2Int tile = TileOf(texel);
                masks.TryGetValue(tile, out int mask);
                masks[tile] = mask | (1 << SquareOf(texel));
            }

            return masks;
        }
    }
}
