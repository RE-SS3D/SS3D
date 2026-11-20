using SS3D.Systems.IdAccess;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.FloorVisuals
{
    /// <summary>
    /// Client side of floor paint (<see cref="FloorPaint"/>): the synced paint, the map editor's preview, and the one
    /// global texture the floor shader reads them from by world position (<c>_FloorPaintMap</c> and
    /// <c>_FloorPaintRect</c> in <c>StationFloorTile.hlsl</c>). A texel per floor square, transparent where unpainted.
    /// <para>
    /// The editor's preview is drawn by the floor shader itself, so it looks exactly like the paint will. A stroke the
    /// editor has sent stays drawn (pending) until the server's paint arrives, so painting never flickers back.
    /// </para>
    /// </summary>
    public static class FloorPaintView
    {
        private const int ChunkTexels = TileChunk.ChunkSize * 2;
        private const float PendingSeconds = 3f;

        private static readonly int MapId = Shader.PropertyToID("_FloorPaintMap");
        private static readonly int RectId = Shader.PropertyToID("_FloorPaintRect");

        private static readonly Dictionary<Vector2Int, ushort[]> Chunks = new();
        private static readonly Dictionary<Vector2Int, byte> Pending = new();
        private static readonly Dictionary<Vector2Int, byte> Preview = new();
        private static float _pendingSince;

        // Each palette index's colour, read once per rebuild rather than once per square.
        private static readonly Color32[] Colors = new Color32[16];

        private static Texture2D _texture;
        private static Color32[] _base;
        private static Color32[] _pixels;
        // The texture's texels in world half-tiles.
        private static RectInt _rect;

        // A department colour edited in Project Settings redraws the floors with it.
        static FloorPaintView() => DepartmentColors.Changed += () => Rebuild(rebuildBase: true);

        /// <summary>The server's paint, chunk by chunk; replaces everything this client had.</summary>
        public static void ReplaceChunks(IReadOnlyList<(Vector2Int chunkKey, ushort[] floorPaint)> chunks)
        {
            Chunks.Clear();
            Pending.Clear();
            if (chunks != null)
            {
                foreach ((Vector2Int chunkKey, ushort[] floorPaint) in chunks)
                {
                    if (floorPaint != null && floorPaint.Length == TileChunk.ChunkSize * TileChunk.ChunkSize)
                        Chunks[chunkKey] = (ushort[])floorPaint.Clone();
                }
            }

            Rebuild(rebuildBase: true);
        }

        /// <summary>The palette index on a square as the server last sent it (preview not included).</summary>
        public static int IndexAt(Vector2Int texel)
        {
            Vector2Int tile = FloorPaint.TileOf(texel);
            Vector2Int key = new(FloorDiv(tile.x, TileChunk.ChunkSize), FloorDiv(tile.y, TileChunk.ChunkSize));
            if (!Chunks.TryGetValue(key, out ushort[] paint))
                return 0;

            int localX = tile.x - key.x * TileChunk.ChunkSize;
            int localY = tile.y - key.y * TileChunk.ChunkSize;
            return FloorPaint.Get(paint[localY * TileChunk.ChunkSize + localX], FloorPaint.SquareOf(texel));
        }

        /// <summary>Shows <paramref name="texels"/> in palette colour <paramref name="index"/> on top of the paint.</summary>
        public static void SetPreview(IReadOnlyList<Vector2Int> texels, byte index)
        {
            Preview.Clear();
            foreach (Vector2Int texel in texels)
                Preview[texel] = index;

            Rebuild(rebuildBase: false);
        }

        public static void ClearPreview()
        {
            if (Preview.Count == 0)
                return;

            Preview.Clear();
            Rebuild(rebuildBase: false);
        }

        /// <summary>The preview was sent to the server: keep showing it until the server's paint replaces it.</summary>
        public static void HoldPreview()
        {
            foreach (KeyValuePair<Vector2Int, byte> texel in Preview)
                Pending[texel.Key] = texel.Value == FloorPaint.ErasePreview ? (byte)0 : texel.Value;

            _pendingSince = Time.realtimeSinceStartup;
            Preview.Clear();
            Rebuild(rebuildBase: false);
        }

        /// <summary>Drops a pending stroke the server never answered (refused, or nothing changed).</summary>
        public static void ExpirePending()
        {
            if (Pending.Count > 0 && Time.realtimeSinceStartup - _pendingSince > PendingSeconds)
            {
                Pending.Clear();
                Rebuild(rebuildBase: false);
            }
        }

        /// <summary>Forget everything: the client left this server.</summary>
        public static void Clear()
        {
            Chunks.Clear();
            Pending.Clear();
            Preview.Clear();
            Shader.SetGlobalVector(RectId, Vector4.zero);
        }

        private static void Rebuild(bool rebuildBase)
        {
            if (!TryGetBounds(out RectInt needed))
            {
                Shader.SetGlobalVector(RectId, Vector4.zero);
                return;
            }

            if (_texture == null || !Contains(_rect, needed))
            {
                // A chunk of room on every side, so painting near the edge doesn't reallocate each stroke.
                _rect = new RectInt(needed.xMin - ChunkTexels, needed.yMin - ChunkTexels,
                    needed.width + 2 * ChunkTexels, needed.height + 2 * ChunkTexels);
                Allocate();
                rebuildBase = true;
            }

            for (int index = 0; index < Colors.Length; index++)
                Colors[index] = FloorPaint.ColorOf(index);

            if (rebuildBase)
                FillBase();

            System.Array.Copy(_base, _pixels, _base.Length);
            foreach (KeyValuePair<Vector2Int, byte> texel in Pending)
                Write(texel.Key, texel.Value);
            foreach (KeyValuePair<Vector2Int, byte> texel in Preview)
                Write(texel.Key, texel.Value);

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            Shader.SetGlobalTexture(MapId, _texture);
            Shader.SetGlobalVector(RectId, new Vector4(_rect.x, _rect.y, 1f / _rect.width, 1f / _rect.height));
        }

        private static void Allocate()
        {
            if (_texture != null && UnityEngine.Application.isPlaying)
                Object.Destroy(_texture);
            else if (_texture != null)
                Object.DestroyImmediate(_texture);

            // sRGB, like the palette's hex colours; point sampled, one texel per square.
            _texture = new Texture2D(_rect.width, _rect.height, TextureFormat.RGBA32, false, false)
            {
                name = "FloorPaint",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            _base = new Color32[_rect.width * _rect.height];
            _pixels = new Color32[_base.Length];
        }

        private static void FillBase()
        {
            System.Array.Clear(_base, 0, _base.Length);
            foreach (KeyValuePair<Vector2Int, ushort[]> chunk in Chunks)
            {
                for (int localY = 0; localY < TileChunk.ChunkSize; localY++)
                {
                    for (int localX = 0; localX < TileChunk.ChunkSize; localX++)
                    {
                        ushort paint = chunk.Value[localY * TileChunk.ChunkSize + localX];
                        if (paint == 0)
                            continue;

                        Vector2Int tile = new(chunk.Key.x * TileChunk.ChunkSize + localX, chunk.Key.y * TileChunk.ChunkSize + localY);
                        for (int square = 0; square < 4; square++)
                        {
                            int index = FloorPaint.Get(paint, square);
                            if (index != 0)
                                _base[PixelOf(FloorPaint.TexelOf(tile, square))] = Colors[index];
                        }
                    }
                }
            }
        }

        private static void Write(Vector2Int texel, byte index)
        {
            if (texel.x < _rect.xMin || texel.y < _rect.yMin || texel.x >= _rect.xMax || texel.y >= _rect.yMax)
                return;

            int pixel = PixelOf(texel);
            // Erasing shows the floor's own colour, or the erase colour where the preview marks squares to erase.
            _pixels[pixel] = Colors[index & 0xF];
        }

        private static int PixelOf(Vector2Int texel) => (texel.y - _rect.y) * _rect.width + (texel.x - _rect.x);

        private static bool TryGetBounds(out RectInt bounds)
        {
            int xMin = int.MaxValue, yMin = int.MaxValue, xMax = int.MinValue, yMax = int.MinValue;
            foreach (Vector2Int key in Chunks.Keys)
            {
                xMin = Mathf.Min(xMin, key.x * ChunkTexels);
                yMin = Mathf.Min(yMin, key.y * ChunkTexels);
                xMax = Mathf.Max(xMax, (key.x + 1) * ChunkTexels);
                yMax = Mathf.Max(yMax, (key.y + 1) * ChunkTexels);
            }

            Grow(Pending.Keys, ref xMin, ref yMin, ref xMax, ref yMax);
            Grow(Preview.Keys, ref xMin, ref yMin, ref xMax, ref yMax);
            bounds = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            return xMin < xMax;
        }

        private static void Grow(IEnumerable<Vector2Int> texels, ref int xMin, ref int yMin, ref int xMax, ref int yMax)
        {
            foreach (Vector2Int texel in texels)
            {
                xMin = Mathf.Min(xMin, texel.x);
                yMin = Mathf.Min(yMin, texel.y);
                xMax = Mathf.Max(xMax, texel.x + 1);
                yMax = Mathf.Max(yMax, texel.y + 1);
            }
        }

        private static bool Contains(RectInt outer, RectInt inner) =>
            inner.xMin >= outer.xMin && inner.yMin >= outer.yMin && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;

        private static int FloorDiv(int value, int divisor) => value >= 0 ? value / divisor : -((-value + divisor - 1) / divisor);
    }
}
