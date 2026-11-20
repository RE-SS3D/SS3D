using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Ghosts of one prefab laid over a set of cells, pooled so a drag that grows and shrinks doesn't instantiate and
    /// destroy every frame. The room tool keeps one set for its walls and one for its floor.
    /// </summary>
    internal sealed class ConstructionHologramSet
    {
        private readonly List<ConstructionHologram> _active = new();
        private readonly List<ConstructionHologram> _pool = new();

        public int Count => _active.Count;

        public ConstructionHologram this[int index] => _active[index];

        /// <summary>Grow or shrink to <paramref name="count"/> ghosts, reusing pooled ones before creating any.</summary>
        public void Resize(int count, Func<ConstructionHologram> create)
        {
            while (_active.Count < count)
            {
                ConstructionHologram hologram;
                if (_pool.Count > 0)
                {
                    hologram = _pool[_pool.Count - 1];
                    _pool.RemoveAt(_pool.Count - 1);
                    hologram.SetActive = true;
                }
                else
                {
                    hologram = create();
                }

                _active.Add(hologram);
            }

            while (_active.Count > count)
            {
                ConstructionHologram extra = _active[_active.Count - 1];
                _active.RemoveAt(_active.Count - 1);
                extra.SetActive = false;
                _pool.Add(extra);
            }
        }

        /// <summary>Snap ghost <paramref name="index"/> onto <paramref name="cell"/>.</summary>
        public void MoveTo(int index, Vector3 cell)
        {
            ConstructionHologram hologram = _active[index];
            if (hologram.TargetPosition == cell && hologram.ActiveSelf)
                return;

            hologram.TargetPosition = cell;
            hologram.Hologram.transform.position = cell + new Vector3(0f, hologram.PlacementYOffset + 0.1f, 0f);
        }

        /// <summary>Unspawned network prefabs switch themselves off after a few frames; keep the shown ones on.</summary>
        public void KeepActive()
        {
            foreach (ConstructionHologram hologram in _active)
            {
                if (!hologram.ActiveSelf)
                    hologram.SetActive = true;
            }
        }

        public void Clear()
        {
            foreach (ConstructionHologram hologram in _active)
                hologram.Destroy();
            foreach (ConstructionHologram hologram in _pool)
                hologram.Destroy();

            _active.Clear();
            _pool.Clear();
        }
    }
}
