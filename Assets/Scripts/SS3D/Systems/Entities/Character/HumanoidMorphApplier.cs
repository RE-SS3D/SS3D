using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Entities.Character
{
    /// <summary>
    /// Applies Human FBX body shape keys (blend shapes) across a humanoid hierarchy.
    /// Shape names match <c>Assets/Art/Models/Entities/Humanoids/Human/Human.fbx</c>.
    /// </summary>
    public static class HumanoidMorphApplier
    {
        public const string Female = "Female";
        public const string Breasts = "Breasts";
        public const string Fat = "Fat";
        public const string Muscle = "Muscle";
        public const string Jaw = "Jaw";
        public const string JawWide = "Jaw Wide";
        public const string JawThin = "Jaw Thin";

        /// <summary>UI / draft key → blend-shape name for 0–1 weights (excludes bipolar jaw + height).</summary>
        public static readonly IReadOnlyDictionary<string, string> DirectMorphKeys =
            new Dictionary<string, string>
            {
                ["female"] = Female,
                ["breasts"] = Breasts,
                ["fat"] = Fat,
                ["muscle"] = Muscle,
            };

        /// <summary>
        /// Applies body morphs. Values are 0–1. Jaw is bipolar (0 = thin, 0.5 = neutral, 1 = wide).
        /// Height scales <paramref name="heightTarget"/> (default: the root) uniformly; there is no
        /// Height blend shape on the mesh. Networked bodies pass their armature so colliders and the
        /// replicated root keep their size.
        /// </summary>
        public static void Apply(
            GameObject root,
            float female,
            float breasts,
            float fat,
            float muscle,
            float jaw,
            float height,
            Transform heightTarget = null)
        {
            if (root == null)
            {
                return;
            }

            float heightScale = Mathf.Lerp(0.88f, 1.12f, Mathf.Clamp01(height));
            (heightTarget != null ? heightTarget : root.transform).localScale = Vector3.one * heightScale;

            float jawWide = Mathf.Clamp01((jaw - 0.5f) * 2f);
            float jawThin = Mathf.Clamp01((0.5f - jaw) * 2f);

            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                {
                    continue;
                }

                SetWeight(renderer, Female, female);
                SetWeight(renderer, Breasts, breasts);
                SetWeight(renderer, Fat, fat);
                SetWeight(renderer, Muscle, muscle);
                SetWeight(renderer, Jaw, jaw);
                SetWeight(renderer, JawWide, jawWide);
                SetWeight(renderer, JawThin, jawThin);
            }
        }

        private static void SetWeight(SkinnedMeshRenderer renderer, string shapeName, float normalized01)
        {
            Mesh mesh = renderer.sharedMesh;
            int index = mesh.GetBlendShapeIndex(shapeName);
            if (index < 0)
            {
                return;
            }

            renderer.SetBlendShapeWeight(index, Mathf.Clamp01(normalized01) * 100f);
        }
    }
}
