using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Utils
{
    /// <summary>
    /// Bright full-toon UI icons: swaps preview renderers to <c>Unlit/ObjectIcon</c>
    /// and renders via <see cref="RuntimePreviewGenerator"/>.
    /// </summary>
    public static class IconPreviewGenerator
    {
        public const string ObjectIconShaderName = "Unlit/ObjectIcon";

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ColIntenseId = Shader.PropertyToID("_ColIntense");
        private static readonly int ColBrightId = Shader.PropertyToID("_ColBright");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// Renders <paramref name="model"/> as a bright ObjectIcon texture.
        /// When <paramref name="shouldCloneModel"/> is false, the caller owns the transform
        /// (materials are restored afterward). When true, a temporary clone is destroyed.
        /// </summary>
        public static Texture2D Generate(Transform model, int width = 128, int height = 128, bool shouldCloneModel = false)
        {
            if (model == null)
            {
                return null;
            }

            Shader shader = Shader.Find(ObjectIconShaderName);
            if (shader == null)
            {
                Debug.LogWarning(
                    $"{ObjectIconShaderName} missing (stripped?). Add it to Always Included Shaders. Falling back to lit preview.");
                return RuntimePreviewGenerator.GenerateModelPreview(model, width, height, shouldCloneModel);
            }

            Transform preview = model;
            bool ownsClone = false;
            if (shouldCloneModel)
            {
                preview = Object.Instantiate(model, null, false);
                preview.gameObject.hideFlags = HideFlags.HideAndDontSave;
                ownsClone = true;
            }

            var originals = new Dictionary<Renderer, Material[]>();
            var tempMaterials = new List<Material>();

            Color previousBackground = RuntimePreviewGenerator.BackgroundColor;
            bool previousOrthographic = RuntimePreviewGenerator.OrthographicMode;

            try
            {
                ApplyObjectIconMaterials(preview, shader, originals, tempMaterials);

                RuntimePreviewGenerator.BackgroundColor = new Color(0f, 0f, 0f, 0f);
                RuntimePreviewGenerator.OrthographicMode = true;

                return RuntimePreviewGenerator.GenerateModelPreview(preview, width, height, false);
            }
            finally
            {
                RuntimePreviewGenerator.BackgroundColor = previousBackground;
                RuntimePreviewGenerator.OrthographicMode = previousOrthographic;

                RestoreMaterials(originals);
                for (int i = 0; i < tempMaterials.Count; i++)
                {
                    if (tempMaterials[i] != null)
                    {
                        Object.DestroyImmediate(tempMaterials[i]);
                    }
                }

                if (ownsClone && preview != null)
                {
                    Object.DestroyImmediate(preview.gameObject);
                }
            }
        }

        private static void ApplyObjectIconMaterials(
            Transform root,
            Shader shader,
            Dictionary<Renderer, Material[]> originals,
            List<Material> tempMaterials)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null || renderer.transform.name == "InteractionOutline")
                {
                    continue;
                }

                Material[] shared = renderer.sharedMaterials;
                originals[renderer] = shared;

                Material[] replacements = new Material[shared.Length];
                for (int m = 0; m < shared.Length; m++)
                {
                    Material source = shared[m];
                    Material iconMat = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    CopyIconProperties(source, iconMat);
                    tempMaterials.Add(iconMat);
                    replacements[m] = iconMat;
                }

                renderer.sharedMaterials = replacements;
            }
        }

        private static void CopyIconProperties(Material source, Material destination)
        {
            if (source == null)
            {
                return;
            }

            if (source.HasProperty(MainTexId))
            {
                destination.SetTexture(MainTexId, source.GetTexture(MainTexId));
                destination.SetTextureScale(MainTexId, source.GetTextureScale(MainTexId));
                destination.SetTextureOffset(MainTexId, source.GetTextureOffset(MainTexId));
            }

            if (source.HasProperty(ColorId))
            {
                Color color = source.GetColor(ColorId);

                // ObjectIcon outputs texture alpha * _Color.a, but opaque world shaders ignore _Color.a.
                // Several floor materials (TileGrey, TileWhite) ship with a: 0 and rendered as blank icons.
                if (source.renderQueue <= (int)UnityEngine.Rendering.RenderQueue.GeometryLast)
                {
                    color.a = 1f;
                }

                destination.SetColor(ColorId, color);
            }

            if (source.HasProperty(ColIntenseId))
            {
                destination.SetFloat(ColIntenseId, source.GetFloat(ColIntenseId));
            }

            if (source.HasProperty(ColBrightId))
            {
                destination.SetFloat(ColBrightId, source.GetFloat(ColBrightId));
            }

            if (source.HasProperty(EmissionMapId))
            {
                destination.SetTexture(EmissionMapId, source.GetTexture(EmissionMapId));
            }

            if (source.HasProperty(EmissionColorId))
            {
                destination.SetColor(EmissionColorId, source.GetColor(EmissionColorId));
            }
        }

        private static void RestoreMaterials(Dictionary<Renderer, Material[]> originals)
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in originals)
            {
                if (pair.Key != null)
                {
                    pair.Key.sharedMaterials = pair.Value;
                }
            }
        }
    }
}
