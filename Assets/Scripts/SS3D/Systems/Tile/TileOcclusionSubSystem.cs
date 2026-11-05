using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Client-side subsystem that manages under-floor tile occlusion.
    /// When a tile and all its 8 neighbors have an occupied Turf layer, coverable layers
    /// (Plenum, Wire, Disposal, Pipe*) have their renderers disabled.
    /// Fully event-driven with zero per-frame Update overhead.
    /// </summary>
    public class TileOcclusionSubSystem : SubSystem
    {
        private readonly Dictionary<Vector2Int, int> _turfCounts = new();
        private readonly Dictionary<Vector2Int, List<TileOcclusionMember>> _coverableMembers = new();
        private readonly HashSet<TileOcclusionMember> _registeredTurfs = new();

        /// <summary>
        /// Checks whether a given tile layer is eligible to be hidden under turfs.
        /// </summary>
        public static bool IsCoverableLayer(TileLayer layer)
        {
            switch (layer)
            {
                case TileLayer.Plenum:
                case TileLayer.Wire:
                case TileLayer.Disposal:
                case TileLayer.PipeSurface:
                case TileLayer.PipeMiddle:
                case TileLayer.PipeLeft:
                case TileLayer.PipeRight:
                    return true;
                default:
                    return false;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            // Self-dispose on headless dedicated servers
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Destroy(this);
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            _turfCounts.Clear();
            _registeredTurfs.Clear();
            _coverableMembers.Clear();
        }

        /// <summary>
        /// Registers a tile member when it spawns on the client.
        /// </summary>
        public void RegisterMember(TileOcclusionMember member, Vector2Int position, TileLayer layer)
        {
            if (layer == TileLayer.Turf)
            {
                if (!_registeredTurfs.Add(member))
                {
                    return;
                }

                _turfCounts.TryGetValue(position, out int count);
                _turfCounts[position] = count + 1;

                // Re-evaluate this position and all 8 neighbors
                UpdateCoverageArea(position);
                return;
            }

            if (!IsCoverableLayer(layer))
            {
                return;
            }

            if (!_coverableMembers.TryGetValue(position, out List<TileOcclusionMember> members))
            {
                members = new List<TileOcclusionMember>();
                _coverableMembers.Add(position, members);
            }

            if (!members.Contains(member))
            {
                members.Add(member);
            }

            // Immediately set visibility based on current coverage state
            bool isCovered = IsFullyCovered(position);
            member.SetVisible(!isCovered);
        }

        /// <summary>
        /// Unregisters a tile member when it is destroyed on the client.
        /// </summary>
        public void UnregisterMember(TileOcclusionMember member, Vector2Int position, TileLayer layer)
        {
            if (layer == TileLayer.Turf)
            {
                if (!_registeredTurfs.Remove(member))
                {
                    return;
                }

                if (_turfCounts.TryGetValue(position, out int count))
                {
                    if (count <= 1)
                    {
                        _turfCounts.Remove(position);
                    }
                    else
                    {
                        _turfCounts[position] = count - 1;
                    }
                }

                // Re-evaluate this position and all 8 neighbors to reveal exposed objects
                UpdateCoverageArea(position);
                return;
            }

            if (!IsCoverableLayer(layer) || !_coverableMembers.TryGetValue(position, out List<TileOcclusionMember> members))
            {
                return;
            }

            members.Remove(member);

            if (members.Count == 0)
            {
                _coverableMembers.Remove(position);
            }
        }

        /// <summary>
        /// Checks if a tile position and all of its 8 surrounding neighbors contain at least one Turf.
        /// </summary>
        public bool IsFullyCovered(Vector2Int position)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!_turfCounts.ContainsKey(new Vector2Int(position.x + dx, position.y + dy)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the visibility of coverable objects on a single tile coordinate.
        /// </summary>
        private void UpdateTileVisibility(Vector2Int position)
        {
            if (!_coverableMembers.TryGetValue(position, out List<TileOcclusionMember> members))
            {
                return;
            }

            bool isCovered = IsFullyCovered(position);
            bool shouldBeVisible = !isCovered;

            for (int i = members.Count - 1; i >= 0; i--)
            {
                TileOcclusionMember member = members[i];
                if (member == null)
                {
                    members.RemoveAt(i);
                    continue;
                }

                member.SetVisible(shouldBeVisible);
            }

            if (members.Count == 0)
            {
                _coverableMembers.Remove(position);
            }
        }

        /// <summary>
        /// Re-evaluates visibility for the target position and all 8 neighbors around it.
        /// </summary>
        private void UpdateCoverageArea(Vector2Int center)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    UpdateTileVisibility(new Vector2Int(center.x + dx, center.y + dy));
                }
            }
        }
    }
}
