using SS3D.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile.FloorVisuals
{
    /// <summary>
    /// Shared helpers for non-networked floor stripe and floor decal mesh quads.
    /// </summary>
    public static class FloorVisualMesh
    {
        public const float SurfaceLift = 0.005f;

        /// <summary>
        /// Station shader material with Alpha Clip on. Loaded from Resources so its
        /// <c>_ALPHATEST_ON</c> variant ships in player builds: a shader_feature keyword
        /// only enabled from code is stripped.
        /// </summary>
        private const string CutoutTemplatePath = "FloorVisuals/FloorVisualCutout";

        private static Mesh _quad;
        private static Texture2D _stripeCornerTexture;
        private static Material _cutoutTemplate;

        public static Mesh GetQuad()
        {
            if (_quad != null)
                return _quad;

            _quad = new Mesh
            {
                name = "FloorVisualQuad",
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f),
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
                normals = new[]
                {
                    Vector3.up,
                    Vector3.up,
                    Vector3.up,
                    Vector3.up,
                },
            };
            _quad.RecalculateBounds();
            return _quad;
        }

        public static Texture2D GetStripeCornerTexture()
        {
            if (_stripeCornerTexture != null)
                return _stripeCornerTexture;

            _stripeCornerTexture = Resources.Load<Texture2D>("FloorVisuals/StripeCorner");
            return _stripeCornerTexture;
        }

        /// <summary>
        /// Alpha-clipped station material for a stripe or decal quad, so it is lit and
        /// occluded (CORTAO) like the floor under it. Texels whose alpha × tint alpha
        /// falls below the template's cutoff are discarded.
        /// </summary>
        public static Material CreateCutoutMaterial(Texture2D texture, Color tint)
        {
            if (_cutoutTemplate == null)
                _cutoutTemplate = Resources.Load<Material>(CutoutTemplatePath);

            Material material;
            if (_cutoutTemplate != null)
            {
                material = new Material(_cutoutTemplate);
            }
            else
            {
                Log.Error(typeof(FloorVisualMesh), "Missing Resources/{path}; floor visuals fall back to Sprites/Default", Logs.Generic, CutoutTemplatePath);
                material = new Material(Shader.Find("Sprites/Default"))
                {
                    renderQueue = (int)RenderQueue.AlphaTest,
                };
            }

            if (texture != null)
                material.mainTexture = texture;

            material.color = tint;
            return material;
        }

        public static GameObject CreateQuadObject(string name, Transform parent, Vector3 worldPosition, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition + Vector3.up * SurfaceLift;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = GetQuad();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }
    }
}
