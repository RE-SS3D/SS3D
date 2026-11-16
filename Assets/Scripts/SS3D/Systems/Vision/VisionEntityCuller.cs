using System.Collections.Generic;
using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Shows people, items and wall mounts whole or not at all (fov.md §4, 2026-11_fov-rework D9). A person or
    /// item shows when any of three points is in sight: the middle of its footprint and both sides of it across
    /// the line of sight. A wall mount shows when the floor tile it faces is in sight. Hidden ones get
    /// <see cref="RendererHiding.Reason.OutOfSight"/>, which also drops their shadows; lights they carry keep
    /// shining. Every renderer handled also carries <see cref="VisionRenderingLayers.Entity"/>, so the mask never
    /// cuts one that is shown. Client only; owned by <see cref="VisionSubSystem"/>.
    /// </summary>
    internal sealed class VisionEntityCuller
    {
        /// <summary>Renderer lists are re-read this often (frames), for clothes put on or items picked up.</summary>
        private const int RefreshInterval = 30;

        private const float MinRadius = 0.1f;
        private const float MaxRadius = 0.6f;

        /// <summary>Height above the floor at which a wall mount's floor tile is tested; any height off the floor works.</summary>
        private const float FloorProbeHeight = 1f;

        private sealed class Target
        {
            public Component Owner;
            public Transform Transform;
            public Renderer[] Renderers = System.Array.Empty<Renderer>();
            public float Radius = MinRadius;
            public float CentreHeight;
            public int NextRefresh;
            public bool Hidden;
            public bool Held;
            public Transform Parent;
            public Vector3 FacingFloor;
            public int SeenFrame;
        }

        private readonly Dictionary<Component, Target> _targets = new(ReferenceComparer<Component>.Instance);

        // Which target decides each renderer, so a holder and the item it drops never undo each other.
        private readonly Dictionary<Renderer, Target> _owners = new(ReferenceComparer<Renderer>.Instance);
        private readonly List<Component> _gone = new();
        private readonly List<Renderer> _scratch = new();
        private readonly List<Renderer> _ownersScratch = new();
        private int _frame;

        public void Cull(VisionSubSystem vision, Entity viewer)
        {
            _frame++;
            Vector3 eye = vision.DetectionCenter;
            float reach = vision.ViewRange + MaxRadius;

            // Index loops: foreach over an IReadOnlyList boxes its enumerator every frame.
            IReadOnlyList<Entity> entities = SceneRegistry<Entity>.All;
            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                if (entity == null)
                    continue;

                // The viewer is tracked too, for the rendering-layer bit, but always shows.
                Target target = Track(entity);
                Apply(target, ReferenceEquals(entity, viewer) || IsBodyVisible(vision, eye, reach, target.Transform.position, target));
            }

            IReadOnlyList<Item> items = SceneRegistry<Item>.All;
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                // Spawned only: inventory icon previews instantiate items that must keep drawing.
                if (item == null || !item.IsSpawned)
                    continue;

                Target target = Track(item);
                if (!target.Held)
                    Apply(target, IsBodyVisible(vision, eye, reach, target.Transform.position, target));
            }

            IReadOnlyList<PlacedTileObject> mounts = ClientWallMounts.All;
            float reachSquared = reach * reach;
            for (int i = 0; i < mounts.Count; i++)
            {
                PlacedTileObject mount = mounts[i];
                if (mount == null)
                    continue;

                Target target = Track(mount);
                Vector3 floor = target.FacingFloor;
                float dx = floor.x - eye.x;
                float dz = floor.z - eye.z;
                Apply(target, (dx * dx) + (dz * dz) <= reachSquared && vision.IsVisible(floor + (Vector3.up * FloorProbeHeight)));
            }

            ForgetGone();
        }

        /// <summary>Shows everything this culler hid, when the field of view turns off.</summary>
        public void ShowAll()
        {
            foreach (Target target in _targets.Values)
                SetHidden(target, false);

            _targets.Clear();
            _owners.Clear();
        }

        private static bool IsBodyVisible(VisionSubSystem vision, Vector3 eye, float reach, Vector3 root, Target target)
        {
            Vector3 centre = root + (Vector3.up * target.CentreHeight);
            Vector3 across = new(centre.z - eye.z, 0f, eye.x - centre.x);
            float distance = across.magnitude;
            if (distance > reach)
                return false;

            if (vision.IsVisible(centre))
                return true;

            if (distance < 1e-3f)
                return false;

            across *= target.Radius / distance;
            return vision.IsVisible(centre + across) || vision.IsVisible(centre - across);
        }

        private Target Track(Component owner)
        {
            if (!_targets.TryGetValue(owner, out Target target))
            {
                target = new Target { Owner = owner, Transform = owner.transform, NextRefresh = _frame };
                _targets.Add(owner, target);
            }

            target.SeenFrame = _frame;
            // An item picked up or dropped changes hands at once.
            if (_frame >= target.NextRefresh || (target.Owner is Item && target.Transform.parent != target.Parent))
                Refresh(target);

            return target;
        }

        private void Refresh(Target target)
        {
            // Stagger re-reads so a station full of items doesn't refresh on one frame.
            target.NextRefresh = _frame + RefreshInterval + (target.Owner.GetInstanceID() & 7);

            _scratch.Clear();
            target.Owner.GetComponentsInChildren(true, _scratch);
            target.Parent = target.Transform.parent;

            // Held and worn items show or hide with whoever carries them: their renderers join the holder's list.
            bool wasHeld = target.Held;
            Entity holder = target.Owner is Item ? target.Owner.GetComponentInParent<Entity>(true) : null;
            target.Held = holder != null;
            if (target.Held)
            {
                _scratch.Clear();
                target.Hidden = false;
                if (_targets.TryGetValue(holder, out Target holderTarget))
                    holderTarget.NextRefresh = _frame;
            }

            foreach (Renderer old in target.Renderers)
            {
                if (old == null || _scratch.Contains(old) || !_owners.TryGetValue(old, out Target owner) || owner != target)
                    continue;

                _owners.Remove(old);
                // Handed to a holder: it decides from its next refresh, so don't flash the item in between.
                if (!target.Held || wasHeld)
                    RendererHiding.Set(old, RendererHiding.Reason.OutOfSight, false);
            }

            Vector3 root = target.Transform.position;
            if (target.Owner is PlacedTileObject mount)
                target.FacingFloor = ClientWallMounts.FacingFloor(mount);

            Bounds bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in _scratch)
            {
                renderer.renderingLayerMask = VisionRenderingLayers.WithEntity(renderer.renderingLayerMask);
                _owners[renderer] = target;
                RendererHiding.Set(renderer, RendererHiding.Reason.OutOfSight, target.Hidden);
                if (!renderer.enabled || renderer is ParticleSystemRenderer)
                    continue;

                if (hasBounds)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;

                hasBounds = true;
            }

            if (!SameRenderers(target.Renderers, _scratch))
                target.Renderers = _scratch.ToArray();
            if (hasBounds)
            {
                target.Radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), MinRadius, MaxRadius);
                target.CentreHeight = bounds.center.y - root.y;
            }
        }

        private static bool SameRenderers(Renderer[] current, List<Renderer> next)
        {
            if (current.Length != next.Count)
                return false;

            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != next[i])
                    return false;
            }

            return true;
        }

        private void Apply(Target target, bool visible)
        {
            if (target.Hidden == !visible)
                return;

            SetHidden(target, !visible);
        }

        private void SetHidden(Target target, bool hidden)
        {
            target.Hidden = hidden;
            foreach (Renderer renderer in target.Renderers)
            {
                if (renderer != null && _owners.TryGetValue(renderer, out Target owner) && owner == target)
                    RendererHiding.Set(renderer, RendererHiding.Reason.OutOfSight, hidden);
            }
        }

        private void PruneOwners()
        {
            _ownersScratch.Clear();
            foreach (KeyValuePair<Renderer, Target> pair in _owners)
            {
                if (pair.Key == null)
                    _ownersScratch.Add(pair.Key);
            }

            foreach (Renderer renderer in _ownersScratch)
                _owners.Remove(renderer);
        }

        private void ForgetGone()
        {
            _gone.Clear();
            foreach (KeyValuePair<Component, Target> pair in _targets)
            {
                if (pair.Key == null || pair.Value.SeenFrame != _frame)
                    _gone.Add(pair.Key);
            }

            foreach (Component owner in _gone)
            {
                Target target = _targets[owner];
                SetHidden(target, false);
                foreach (Renderer renderer in target.Renderers)
                {
                    if (renderer != null && _owners.TryGetValue(renderer, out Target current) && current == target)
                        _owners.Remove(renderer);
                }

                _targets.Remove(owner);
            }

            if (_owners.Count > 4 * (_targets.Count + 64))
                PruneOwners();
        }
    }
}
