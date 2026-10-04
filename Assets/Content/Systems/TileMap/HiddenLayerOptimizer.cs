using System.Collections.Generic;
using Coimbra;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Disables rendering of layers that cannot be seen because they are fully covered by turfs.
    /// A tile's plenum, pipes, disposals and wires layers are hidden when the tile and all 8
    /// neighbouring tiles have an occupied turf layer. See issue RE-SS3D/SS3D#1373.
    /// </summary>
    public class HiddenLayerOptimizer : MonoBehaviour
    {
        private static readonly TileLayer[] HiddenLayers =
        {
            TileLayer.Plenums,
            TileLayer.Pipes,
            TileLayer.Disposals,
            TileLayer.Wires,
        };

        private static readonly Vector2Int[] NeighbourOffsets =
        {
            new Vector2Int(-1, 1),  new Vector2Int(0, 1),  new Vector2Int(1, 1),
            new Vector2Int(-1, 0),                          new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        };

        private TileMapChunk _chunk;
        private readonly Dictionary<TileLayer, Renderer> _layerRenderers = new();

        private void Start()
        {
            _chunk = GetComponent<TileMapChunk>();
            if (_chunk == null)
            {
                Debug.LogError($"[{nameof(HiddenLayerOptimizer)}] requires a {nameof(TileMapChunk)} on the same object.");
                enabled = false;
                return;
            }

            CacheLayerRenderers();
            UpdateHiddenLayers();
        }

        private void CacheLayerRenderers()
        {
            _layerRenderers.Clear();
            foreach (TileLayer layer in HiddenLayers)
            {
                Transform layerObject = transform.Find(layer.ToString());
                if (layerObject != null)
                {
                    _layerRenderers[layer] = layerObject.GetComponent<Renderer>();
                }
            }
        }

        /// <summary>
        /// Recomputes which layers should be rendered. Call after the chunk's tiles change.
        /// </summary>
        public void UpdateHiddenLayers()
        {
            if (_chunk == null)
            {
                return;
            }

            foreach (TileLayer layer in HiddenLayers)
            {
                if (!_layerRenderers.TryGetValue(layer, out Renderer renderer) || renderer == null)
                {
                    continue;
                }

                renderer.enabled = !IsLayerFullyCovered(layer);
            }
        }

        private bool IsLayerFullyCovered(TileLayer layer)
        {
            // The layer can only be hidden if every tile of the chunk has its turf
            // and all 8 neighbours' turfs occupied, so the layer is never visible.
            foreach (Vector2Int tile in GetChunkTiles())
            {
                if (!IsTileAndNeighboursTurfed(tile))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTileAndNeighboursTurfed(Vector2Int tile)
        {
            if (!IsTurfed(tile))
            {
                return false;
            }

            foreach (Vector2Int offset in NeighbourOffsets)
            {
                if (!IsTurfed(tile + offset))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTurfed(Vector2Int tile)
        {
            return _chunk.GetTile(tile.x, tile.y, TileLayer.Turfs) != null;
        }

        private IEnumerable<Vector2Int> GetChunkTiles()
        {
            // Chunk origin is the lower-left corner tile of the chunk.
            Vector2Int origin = _chunk.Origin;
            for (int x = 0; x < TileMapConstants.ChunkSize; x++)
            {
                for (int y = 0; y < TileMapConstants.ChunkSize; y++)
                {
                    yield return new Vector2Int(origin.x + x, origin.y + y);
                }
            }
        }
    }
}
