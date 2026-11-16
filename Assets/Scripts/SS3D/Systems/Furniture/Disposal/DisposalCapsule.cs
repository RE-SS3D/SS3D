using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// A real object riding the disposal network (design doc §5) — not a state flag that resolves
    /// after a timer. Server-owned; not a MonoBehaviour, so many capsules can move independently and
    /// simultaneously without one GameObject/Update() per item.
    /// </summary>
    public sealed class DisposalCapsule
    {
        public Item Item { get; }

        public IDisposalElement Destination { get; }

        public DisposalNetworkId NetworkId { get; }

        /// <summary>Ordered pipe segments from entry to destination, world-space waypoints.</summary>
        public IReadOnlyList<Vector3> Waypoints { get; }

        public IReadOnlyList<DisposalSegmentKey> SegmentPath { get; }

        /// <summary>Index of the waypoint segment currently being traversed.</summary>
        public int CurrentSegmentIndex { get; private set; }

        /// <summary>Distance travelled along the current segment, in world units.</summary>
        public float DistanceIntoSegment { get; private set; }

        public bool HasArrived { get; private set; }

        public DisposalCapsule(
            Item item,
            IDisposalElement destination,
            DisposalNetworkId networkId,
            IReadOnlyList<PlacedTileObject> segmentPath)
        {
            Item = item;
            Destination = destination;
            NetworkId = networkId;

            List<Vector3> waypoints = new(segmentPath.Count);
            List<DisposalSegmentKey> keys = new(segmentPath.Count);
            foreach (PlacedTileObject segment in segmentPath)
            {
                waypoints.Add(segment.transform.position);
                keys.Add(DisposalSegmentKey.From(segment));
            }

            Waypoints = waypoints;
            SegmentPath = keys;
        }

        public Vector3 CurrentPosition
        {
            get
            {
                if (Waypoints.Count == 0)
                {
                    return Item != null ? Item.transform.position : Vector3.zero;
                }

                if (CurrentSegmentIndex >= Waypoints.Count - 1)
                {
                    return Waypoints[Waypoints.Count - 1];
                }

                Vector3 from = Waypoints[CurrentSegmentIndex];
                Vector3 to = Waypoints[CurrentSegmentIndex + 1];
                float segmentLength = Vector3.Distance(from, to);
                float t = segmentLength > 0f ? Mathf.Clamp01(DistanceIntoSegment / segmentLength) : 1f;
                return Vector3.Lerp(from, to, t);
            }
        }

        /// <summary>
        /// Advances the capsule by <paramref name="distance"/> world units. Returns true once it has
        /// reached the final waypoint.
        /// </summary>
        public bool Advance(float distance)
        {
            if (HasArrived || Waypoints.Count <= 1)
            {
                HasArrived = true;
                return true;
            }

            float remaining = distance;
            while (remaining > 0f && CurrentSegmentIndex < Waypoints.Count - 1)
            {
                Vector3 from = Waypoints[CurrentSegmentIndex];
                Vector3 to = Waypoints[CurrentSegmentIndex + 1];
                float segmentLength = Vector3.Distance(from, to);
                float distanceLeftInSegment = segmentLength - DistanceIntoSegment;

                if (remaining < distanceLeftInSegment)
                {
                    DistanceIntoSegment += remaining;
                    remaining = 0f;
                }
                else
                {
                    remaining -= distanceLeftInSegment;
                    CurrentSegmentIndex++;
                    DistanceIntoSegment = 0f;
                }
            }

            if (CurrentSegmentIndex >= Waypoints.Count - 1)
            {
                HasArrived = true;
            }

            return HasArrived;
        }

        /// <summary>
        /// True if this capsule's route still has to pass through the given segment (used by the
        /// pipe-cut sabotage check — design doc §7 — to decide whether the item is still inside it).
        /// </summary>
        public bool HasNotYetPassed(DisposalSegmentKey segment)
        {
            for (int i = CurrentSegmentIndex; i < SegmentPath.Count; i++)
            {
                if (SegmentPath[i].Equals(segment))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
