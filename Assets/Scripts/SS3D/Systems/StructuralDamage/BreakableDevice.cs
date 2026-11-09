using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Localization;
using SS3D.Systems.Audio;
using SS3D.Systems.Electricity;
using SS3D.Systems.Examine;
using UnityEngine;
using UnityEngine.Rendering;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// A machine that rounds, swings and blasts wear down until it breaks and stays broken on its mount
    /// (explosives-destruction.md §3, "a dead APC"). It holds no air, so there are no staged warnings beyond a
    /// dented casing. The owning machine reads <see cref="IsBroken"/> and stops working; repair is a later pass.
    /// </summary>
    public sealed class BreakableDevice : NetworkActor, IExamineContentProvider
    {
        private const float HitVolume = 0.8f;
        private const float BreakVolume = 1f;
        private const float MinRange = 2f;
        private const float MaxRange = 30f;

        /// <summary>Below this share of integrity the casing reads as dented on examine.</summary>
        public const float DentedFraction = 0.5f;

        [SerializeField]
        private float _maxIntegrity = 60f;
        [SerializeField]
        private Renderer[] _emissiveRenderers;

        [SyncVar]
        private float _damage;

        [SyncVar(OnChange = nameof(SyncBroken))]
        private bool _broken;

        private EmissiveMaterialSlots _emissiveSlots;

        /// <summary>Server and clients: raised when the device breaks (or is put back).</summary>
        public event Action<bool> OnBrokenChanged;

        public bool IsBroken => _broken;

        public float MaxIntegrity => _maxIntegrity;

        public float Integrity => Mathf.Max(0f, _maxIntegrity - _damage);

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyBrokenVisual();
        }

        /// <summary>
        /// Server / EditMode: wear the casing down by <paramref name="force"/>. Returns true when the hit landed
        /// on a working device (a broken one just takes the round).
        /// </summary>
        public bool ServerApplyDamage(float force, StructuralDamageSource source, Vector3 point = default, Vector3 normal = default)
        {
            if (_broken || force <= 0f || !CanWrite())
            {
                return false;
            }

            _damage = Mathf.Min(_maxIntegrity, _damage + force);
            bool broke = _damage >= _maxIntegrity;
            if (broke)
            {
                _broken = true;
                ApplyBrokenVisual();
                OnBrokenChanged?.Invoke(true);
            }

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                Vector3 at = normal.sqrMagnitude > 0.0001f ? point : transform.position + (Vector3.up * 0.5f);
                Vector3 facing = normal.sqrMagnitude > 0.0001f ? normal : transform.forward;
                PlaySound(broke ? StructuralAudioTrackIds.MetalBreak : StructuralAudioTrackIds.MetalHit, at,
                    broke ? BreakVolume : HitVolume);
                RpcImpact(at, facing, broke);
            }

            return true;
        }

        /// <summary>Server / EditMode: put the device back in working order.</summary>
        public void ServerRepair()
        {
            if (!CanWrite())
            {
                return;
            }

            bool wasBroken = _broken;
            _damage = 0f;
            _broken = false;
            ApplyBrokenVisual();
            if (wasBroken)
            {
                OnBrokenChanged?.Invoke(false);
            }
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (_broken)
            {
                sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(ExamineCanonicalKeyGenerator.ExamineTableName,
                    ExamineStructuralIntegrityKeys.BrokenDevice, null, ExamineStructuralIntegrityKeys.BrokenDeviceFallback)));
            }
            else if (Integrity < _maxIntegrity * DentedFraction)
            {
                sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(ExamineCanonicalKeyGenerator.ExamineTableName,
                    ExamineStructuralIntegrityKeys.DentedDevice, null, ExamineStructuralIntegrityKeys.DentedDeviceFallback)));
            }
        }

        // Same guard as PowerStatus: never write SyncVars on a pure client, but allow EditMode / unspawned.
        private bool CanWrite() => NetworkObject == null || !NetworkObject.IsSpawned || IsServer;

        private void SyncBroken(bool previous, bool next, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            ApplyBrokenVisual();
            OnBrokenChanged?.Invoke(next);
        }

        private void ApplyBrokenVisual()
        {
            if (_emissiveSlots == null)
            {
                _emissiveSlots = new EmissiveMaterialSlots();
                _emissiveSlots.Cache(_emissiveRenderers != null && _emissiveRenderers.Length > 0
                    ? _emissiveRenderers
                    : GetComponentsInChildren<Renderer>(true));
            }

            _emissiveSlots.ApplyPowered(!_broken);
        }

        // RunLocally so the host sees it; a dedicated server has no graphics device to build particles with.
        [ObserversRpc(RunLocally = true)]
        private void RpcImpact(Vector3 point, Vector3 normal, bool broke)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            SteelWallDamageVfx.PlayImpact(point, normal);
            if (broke)
            {
                SteelWallDamageVfx.PlaySparks(point, normal);
            }
        }

        private static void PlaySound(string[] clips, Vector3 position, float volume)
        {
            if (!SubSystems.TryGet(out AudioSubSystem audio))
            {
                return;
            }

            audio.PlayAudioSource(AudioType.Sfx, clips[UnityEngine.Random.Range(0, clips.Length)], position, null, false,
                volume, UnityEngine.Random.Range(0.92f, 1.08f), MinRange, MaxRange);
        }
    }
}
