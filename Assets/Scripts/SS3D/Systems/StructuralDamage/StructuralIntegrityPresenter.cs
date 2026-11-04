using System.Collections.Generic;
using SS3D.Systems.Tile;
using SS3D.Systems.Audio;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Client-visible structural stage feedback: Steel wall overlays, fallback tint, and optional Cracked hiss.
    /// Driven by <see cref="PlacedTileObject"/> integrity SyncVar / local writes — no HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StructuralIntegrityPresenter : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color IntactTint = Color.white;
        private static readonly Color DamagedTint = new Color(0.78f, 0.74f, 0.68f, 1f);
        private static readonly Color CrackedTint = new Color(0.62f, 0.42f, 0.38f, 1f);

        [SerializeField]
        private AudioClip _crackedHiss;

        [SerializeField]
        [Range(0f, 1f)]
        private float _hissVolume = 0.35f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private AudioSource _hissSource;
        private List<(MeshRenderer Surface, MeshRenderer Overlay)> _wallOverlays;
        private StructuralIntegrityStage _applied = StructuralIntegrityStage.Intact;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            CacheRenderers();
        }

        private void OnDisable()
        {
            StopHiss();
        }

        /// <summary>Fallback tint for structural surfaces without a steel wall overlay.</summary>
        public static Color ResolveTint(StructuralIntegrityStage stage)
        {
            return stage switch
            {
                StructuralIntegrityStage.Damaged => DamagedTint,
                StructuralIntegrityStage.Cracked => CrackedTint,
                StructuralIntegrityStage.Destroyed => CrackedTint,
                _ => IntactTint,
            };
        }

        public void Apply(StructuralIntegrityStage stage)
        {
            if (_block == null)
                _block = new MaterialPropertyBlock();

            if (_renderers == null || _renderers.Length == 0)
                CacheRenderers();

            _applied = stage;

            if (TryGetComponent(out PlacedTileObject wall) && SteelWallDamageVfx.SupportsOverlay(wall))
            {
                ApplyWallOverlays(stage);
                UpdateHiss(stage);
                return;
            }

            // Intact: clear MPB so undamaged walls stay SRP Batcher / GPU Instancing friendly.
            if (stage == StructuralIntegrityStage.Intact)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    Renderer renderer = _renderers[i];
                    if (renderer != null)
                        renderer.SetPropertyBlock(null);
                }

                StopHiss();
                return;
            }

            Color tint = ResolveTint(stage);
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_block);
                _block.SetColor(ColorId, tint);
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
                    _block.SetColor(BaseColorId, tint);

                renderer.SetPropertyBlock(_block);
            }

            UpdateHiss(stage);
        }

        private void ApplyWallOverlays(StructuralIntegrityStage stage)
        {
            bool marked = stage is StructuralIntegrityStage.Damaged or StructuralIntegrityStage.Cracked;
            if (marked && _wallOverlays == null)
            {
                _wallOverlays = new();
                foreach (Renderer renderer in _renderers)
                {
                    if (renderer is MeshRenderer surface
                        && TryGetComponent(out PlacedTileObject placed)
                        && SteelWallDamageVfx.IsDamageSurface(surface, placed)
                        && SteelWallDamageVfx.CreateOverlay(surface) is { } overlay)
                        _wallOverlays.Add((surface, overlay));
                }
            }

            if (_wallOverlays == null)
                return;

            foreach (var (_, overlay) in _wallOverlays)
                overlay.gameObject.SetActive(marked);

            RefreshWallOverlayMesh();
        }

        internal void RefreshWallOverlayMesh()
        {
            if (_wallOverlays == null || _applied is not (StructuralIntegrityStage.Damaged or StructuralIntegrityStage.Cracked))
                return;

            foreach (var (surface, overlay) in _wallOverlays)
            {
                if (surface.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    SteelWallDamageVfx.SetStage(overlay, filter.sharedMesh, _applied == StructuralIntegrityStage.Cracked);
                    if (TryGetComponent(out PlacedTileObject placed))
                        SteelWallDamageVfx.ConfigureSurface(overlay, filter.sharedMesh, placed);
                }
            }

            SyncWallOverlayVisibility();
        }

        private void LateUpdate()
        {
            if (_applied is StructuralIntegrityStage.Damaged or StructuralIntegrityStage.Cracked)
                SyncWallOverlayVisibility();
        }

        private void SyncWallOverlayVisibility()
        {
            if (_wallOverlays == null)
                return;

            // Construction variants and host AOI can toggle the source renderer independently.
            foreach (var (surface, overlay) in _wallOverlays)
                overlay.forceRenderingOff = !surface.enabled || surface.forceRenderingOff;
        }

        private void UpdateHiss(StructuralIntegrityStage stage)
        {
            if (stage == StructuralIntegrityStage.Cracked
                && TryGetComponent(out PlacedTileObject placed) && placed.GenericType == TileObjectGenericType.Wall)
                EnsureHissPlaying();
            else
                StopHiss();
        }

        public StructuralIntegrityStage AppliedStage => _applied;

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void EnsureHissPlaying()
        {
            if (_crackedHiss == null || !isActiveAndEnabled)
                return;

            if (_hissSource == null)
            {
                _hissSource = gameObject.AddComponent<AudioSource>();
                _hissSource.playOnAwake = false;
                _hissSource.loop = true;
                _hissSource.spatialBlend = 1f;
                _hissSource.rolloffMode = AudioRolloffMode.Linear;
                _hissSource.minDistance = 1f;
                _hissSource.maxDistance = 12f;
                _hissSource.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.SfxGroup);
            }

            if (_hissSource.isPlaying && _hissSource.clip == _crackedHiss)
                return;

            _hissSource.clip = _crackedHiss;
            _hissSource.volume = _hissVolume;
            _hissSource.Play();
        }

        private void StopHiss()
        {
            if (_hissSource != null && _hissSource.isPlaying)
                _hissSource.Stop();
        }
    }
}
