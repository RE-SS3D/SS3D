using System;
using SS3D.Systems.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Packaged-client checks for bullet-hole decals. <see cref="Validate"/> fails if the build
    /// stripped the DBuffer projector pass (issue #111: holes drawn with URP's magenta error material).
    /// </summary>
    public static class BulletDecalProbe
    {
        /// <summary>
        /// Stamps bullet holes on whatever the main camera sees (a grid of viewport rays), so a
        /// following screenshot shows real receivers, shaders and renderer settings.
        /// </summary>
        public static string StampVisibleSurfaces()
        {
            Camera camera = Camera.main;
            if (camera == null)
                throw new InvalidOperationException("No main camera to stamp bullet holes from.");

            int stamped = 0;
            for (int x = 1; x < 8; x++)
            {
                for (int y = 1; y < 8; y++)
                {
                    Ray ray = camera.ViewportPointToRay(new Vector3(x / 8f, y / 8f, 0f));
                    if (!Physics.Raycast(ray, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                        continue;

                    BulletHoleDecalSpawner.Spawn(hit.point, hit.normal);
                    stamped++;
                }
            }

            return $"stamped={stamped}, camera={camera.name}";
        }

        public static string Validate()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Bullet decal probe requires a graphical client.");

            BulletHoleVfxCatalog catalog = BulletHoleVfxCatalog.Instance;
            if (catalog == null)
                throw new InvalidOperationException("Bullet-hole Resources catalog is missing.");

            Material material = catalog.CreateDecalMaterial();
            if (material == null || material.shader == null || !material.shader.isSupported)
                throw new InvalidOperationException("Bullet-hole decal shader is missing or unsupported.");

            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(128, 128, 0, RenderTextureFormat.ARGB32);
            Texture2D pixels = new(128, 128, TextureFormat.RGBA32, false);
            try
            {
                int projectorPass = material.FindPass("DBufferProjector");
                if (projectorPass < 0)
                    throw new InvalidOperationException("Bullet-hole DBuffer projector pass was stripped.");

                Graphics.Blit(material.GetTexture("Base_Map"), target);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                pixels.Apply();
                int visible = 0;
                int magenta = 0;
                foreach (Color color in pixels.GetPixels())
                {
                    if (color.a < 0.1f) continue;
                    visible++;
                    if (color.r > 0.8f && color.g < 0.2f && color.b > 0.8f) magenta++;
                }

                if (visible == 0 || magenta != 0)
                    throw new InvalidOperationException($"Bullet-hole tint is invalid: visible={visible}, magenta={magenta}.");

                return $"api={SystemInfo.graphicsDeviceType}, shader={material.shader.name}, passes={material.passCount}, projectorPass={projectorPass}, visible={visible}, magenta={magenta}";
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                CoreUtils.Destroy(pixels);
                CoreUtils.Destroy(material);
            }
        }
    }
}
