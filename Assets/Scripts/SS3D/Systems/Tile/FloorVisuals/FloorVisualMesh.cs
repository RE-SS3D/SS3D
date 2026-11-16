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

        private static Mesh _quad;
        private static Texture2D _stripeCornerTexture;

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

        public static Material CreateCutoutMaterial(Texture2D texture, Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.mainTexture = texture;
            }

            material.SetColor("_BaseColor", tint);
            material.color = tint;
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
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
