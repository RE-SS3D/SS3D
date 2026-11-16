#if UNITY_EDITOR
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Does an item lie flat at rest? Its rendered bounds at the rest pose must be thinnest vertically, unless the item
    /// is flagged <see cref="Item.RestsUpright"/> (Documents/architecture/systems/inventory.md). "Longest axis
    /// horizontal" is not enough: the M4 at <c>-90° X</c> passed it while standing on its grip and magazine.
    /// </summary>
    public static class ItemRestPoseAudit
    {
        /// <summary>
        /// How much taller than its thinnest horizontal side an item may be and still count as flat. A tool lying on its
        /// side is often a little taller than wide (the screwdriver's handle: 1.15); a can or an item on its edge is
        /// far more (1.4 and up).
        /// </summary>
        public const float FlatTolerance = 1.25f;

        public static bool LiesFlat(Vector3 size) => size.y <= Mathf.Min(size.x, size.z) * FlatTolerance;

        /// <summary>
        /// Size of the item's visible renderers placed at its rest pose, as <see cref="ItemPoseRenderer.RenderFloor"/>
        /// shows it. False when nothing renders.
        /// </summary>
        public static bool TryRestSize(Item prefab, out Vector3 size)
        {
            size = default;
            Item instance = Object.Instantiate(prefab);
            instance.gameObject.hideFlags = HideFlags.DontSave;
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, ItemPlacement.WorldRotation(prefab, 0f));
                instance.SetVisibility(true);
                bool any = false;
                Bounds bounds = default;
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (renderer is ParticleSystemRenderer || !renderer.enabled)
                    {
                        continue;
                    }

                    if (!any)
                    {
                        bounds = renderer.bounds;
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                size = bounds.size;
                return any;
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        /// <summary>Why the item's rest pose fails the audit, or null when it lies flat, is flagged upright or has no mesh.</summary>
        public static string Problem(Item prefab)
        {
            if (prefab.RestsUpright || !TryRestSize(prefab, out Vector3 size) || LiesFlat(size))
            {
                return null;
            }

            return $"stands {size.y:0.00} m tall on a {Mathf.Min(size.x, size.z):0.00} m side at rest; "
                + "lay it flat or flag it Rests Upright";
        }
    }
}
#endif
