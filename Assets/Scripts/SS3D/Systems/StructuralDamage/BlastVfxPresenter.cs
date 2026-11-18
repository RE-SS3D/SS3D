using SS3D.Core;
using SS3D.Systems.Audio;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Client-side blast detonation presentation. Driven by TileSubSystem ObserversRpc — not the sim.
    /// </summary>
    public static class BlastVfxPresenter
    {
        /// <param name="flameTiles">The tiles the blast reached, from <see cref="BlastFlameTiles.Pack"/>; empty for none.</param>
        public static void Play(Vector3 epicenter, float yield, byte[] flameTiles = null)
        {
            BlastVfxCatalog catalog = BlastVfxCatalog.Instance;
            float yieldScale = BlastVfxFalloff.YieldScale(yield);
            float violence = BlastVfxFalloff.Violence(yield);

            SpawnBurst(epicenter, yieldScale, violence, catalog);
            BlastFlameFront.Spawn(epicenter, flameTiles);
            BlastScorchSpawner.SpawnAt(epicenter, yieldScale, catalog);
            ApplyLocalFeedback(epicenter, yieldScale, violence, catalog);
        }

        private static void SpawnBurst(Vector3 epicenter, float yieldScale, float violence, BlastVfxCatalog catalog)
        {
            Vector3 position = epicenter + Vector3.up * 0.55f;
            GameObject root;

            if (catalog != null && catalog.BurstPrefab != null)
            {
                root = Object.Instantiate(catalog.BurstPrefab, position, Quaternion.identity);
            }
            else
            {
                root = new GameObject("BlastExplosion");
                root.transform.position = position;
                root.AddComponent<BlastExplosionEffect>();
            }

            BlastExplosionEffect effect = root.GetComponent<BlastExplosionEffect>();
            if (effect == null)
                effect = root.AddComponent<BlastExplosionEffect>();

            effect.Play(yieldScale, catalog, violence);
        }

        private static void ApplyLocalFeedback(Vector3 epicenter, float yieldScale, float violence, BlastVfxCatalog catalog)
        {
            // A violent blast is felt and seen further away, not only harder up close.
            float shakeFull = (catalog != null ? catalog.ShakeFullRange : 6f) * violence;
            float shakeMax = (catalog != null ? catalog.ShakeMaxRange : 24f) * violence;
            float flashFull = (catalog != null ? catalog.FlashFullRange : 4f) * violence;
            float flashMax = (catalog != null ? catalog.FlashMaxRange : 20f) * violence;

            if (!TryGetListenerPosition(out Vector3 listener))
                return;

            float distance = Vector3.Distance(listener, epicenter);

            float shakeAttenuation = BlastVfxFalloff.DistanceAttenuation(distance, shakeFull, shakeMax);
            if (shakeAttenuation > 0.01f)
            {
                CameraFollow follow = SceneRegistry<CameraFollow>.Count > 0 ? SceneRegistry<CameraFollow>.All[0] : null;
                if (follow != null)
                {
                    // Mostly longer, barely wider: the shake is already metres wide at a big charge's full range.
                    float amplitude = (catalog != null ? catalog.ShakeAmplitude : 0.75f) * yieldScale
                        * Mathf.Sqrt(violence) * shakeAttenuation;
                    float duration = (catalog != null ? catalog.ShakeDuration : 0.85f) * violence * violence;
                    follow.AddImpulse(amplitude, duration);
                }
            }

            float flashAttenuation = BlastVfxFalloff.DistanceAttenuation(distance, flashFull, flashMax);
            if (flashAttenuation > 0.01f
                && SubSystems.TryGet(out ScreenEffectsSubSystem screenEffects))
            {
                screenEffects.TriggerBlastFlash(flashAttenuation * Mathf.Clamp01(0.65f + 0.35f * yieldScale) * violence,
                    violence);
            }
        }

        private static bool TryGetListenerPosition(out Vector3 position)
        {
            // The listener follows the controlled body; the orbiting camera can be 15 m away.
            if (AudioSourceOcclusion.TryGetListenerPosition(out position))
                return true;
            if (SubSystems.TryGet(out CameraSubSystem cameras) && cameras.PlayerCamera != null)
            {
                position = cameras.PlayerCamera.Position;
                return true;
            }

            Camera main = Camera.main;
            if (main != null)
            {
                position = main.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
