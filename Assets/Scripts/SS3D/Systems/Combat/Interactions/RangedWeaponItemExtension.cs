using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using System.Collections.Generic;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Dedicated hitscan firearm on a held item. Mag/recoil/cooldown/reload are authoritative on
    /// the server instance of this component; owner clients mirror via InteractionController TargetRpcs.
    /// </summary>
    public class RangedWeaponItemExtension : MonoBehaviour, IInteractionSourceExtension
    {
        [SerializeField] private RangedWeaponProfile _profile = RangedWeaponProfile.M4;
        [Tooltip("Barrel tip — muzzle flash and future aim alignment. Wired by RangedPrefabSetup.")]
        [SerializeField] private Transform _muzzle;

        [Header("Reload magazine handoff")]
        [Tooltip("Detachable magazine mesh (child of the weapon). The wielder's left hand carries it during the reload clip. Null keeps it on the gun.")]
        [SerializeField] private Transform _magazine;
        [Tooltip("Magazine position in the left hand bone's space while carried. M4 value is the pose where the reload clip's hand seats the mag.")]
        [SerializeField] private Vector3 _magazineHandPosition = new Vector3(-0.0262f, 0.0370f, -0.0604f);
        [Tooltip("Magazine rotation (Euler) in the left hand bone's space while carried.")]
        [SerializeField] private Vector3 _magazineHandEuler = new Vector3(356.06f, 298.24f, 93.93f);

        private int _rounds;
        private float _recoilStacks;
        private float _cooldownUntil;
        private float _reloadUntil;
        private float _reloadStartedAt;
        /// <summary>Reload sound cues already played this reload: 0 none, 1 mag in, 2 bolt release.</summary>
        private int _reloadCuesPlayed;
        private float _lastRecoilDecayTime;
        private bool _initialized;
        private NetworkObject _networkObject;

        public RangedWeaponProfile Profile => _profile;

        /// <summary>Barrel tip transform when the prefab recipe has wired one; otherwise null.</summary>
        public Transform Muzzle => _muzzle != null ? _muzzle : transform.Find("Muzzle");

        /// <summary>Detachable magazine mesh, or null when the weapon has none.</summary>
        public Transform Magazine => _magazine;

        /// <summary>Carried magazine position in the left hand bone's local space.</summary>
        public Vector3 MagazineHandPosition => _magazineHandPosition;

        /// <summary>Carried magazine rotation in the left hand bone's local space.</summary>
        public Quaternion MagazineHandRotation => Quaternion.Euler(_magazineHandEuler);

        public int RoundsRemaining => _rounds;

        public int MagazineSize => Mathf.Max(1, _profile.MagazineSize);

        public bool IsReloading => _reloadUntil > 0f && Time.time < _reloadUntil;

        public bool IsOnCooldown => Time.time < _cooldownUntil;

        public float RecoilStacks => _recoilStacks;

        public float ReadyProgress01
        {
            get
            {
                float now = Time.time;
                float busyUntil = Mathf.Max(_cooldownUntil, _reloadUntil);
                if (busyUntil <= now)
                {
                    return 1f;
                }

                float duration = IsReloading
                    ? Mathf.Max(0.01f, _profile.ReloadSeconds)
                    : Mathf.Max(0.01f, _profile.FireCooldownSeconds);
                float remaining = busyUntil - now;
                float elapsed = duration - remaining;
                return Mathf.Clamp01(elapsed / duration);
            }
        }

        public bool IsBusy => IsReloading || IsOnCooldown;

        private void Awake()
        {
            _networkObject = GetComponent<NetworkObject>();
            EnsureInitialized();
        }

        private void Update()
        {
            // Reload sound cues and completion fire even if nothing else polls this frame.
            PlayDueReloadCues();
            TryCompleteReloadIfDue();
        }

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            // Fire is Harm-primary on InteractionController — not a discovered Hit.
            if (!CanStartReload())
            {
                return;
            }

            interactions.Add(InteractionEntry.SourceOnly(new ReloadRangedInteraction(this)));
        }

        public bool CanStartFire()
        {
            EnsureInitialized();
            DecayRecoil();
            TryCompleteReloadIfDue();
            return !IsBusy && _rounds > 0;
        }

        public bool CanStartReload()
        {
            EnsureInitialized();
            TryCompleteReloadIfDue();
            return !IsReloading && _rounds < MagazineSize;
        }

        public void BeginLocalFireCooldown()
        {
            float cooldown = Mathf.Max(0.01f, _profile.FireCooldownSeconds);
            _cooldownUntil = Time.time + cooldown;
        }

        public void BeginLocalReload(float reloadSeconds)
        {
            _reloadUntil = Time.time + Mathf.Max(0.01f, reloadSeconds);
        }

        public void ClientSetRounds(int rounds)
        {
            EnsureInitialized();
            _rounds = Mathf.Clamp(rounds, 0, MagazineSize);
        }

        public void ClientSetRecoil(float recoilStacks)
        {
            _recoilStacks = Mathf.Max(0f, recoilStacks);
        }

        public bool ServerTryConsumeRound()
        {
            EnsureInitialized();
            DecayRecoil();
            TryCompleteReloadIfDue();
            if (IsBusy || _rounds <= 0)
            {
                return false;
            }

            _rounds--;
            _recoilStacks += Mathf.Max(0f, _profile.RecoilPerShot);
            float cooldown = Mathf.Max(0.01f, _profile.FireCooldownSeconds);
            _cooldownUntil = Time.time + cooldown;
            return true;
        }

        public bool ServerTryBeginReload()
        {
            EnsureInitialized();
            TryCompleteReloadIfDue();
            if (!CanStartReload())
            {
                return false;
            }

            _reloadStartedAt = Time.time;
            _reloadCuesPlayed = 0;
            _reloadUntil = _reloadStartedAt + Mathf.Max(0.01f, _profile.ReloadSeconds);
            return true;
        }

        /// <summary>Completes a due reload. On server, first plays any reload sound cue still pending.</summary>
        public bool TryCompleteReloadIfDue()
        {
            if (_reloadUntil <= 0f)
            {
                return false;
            }

            if (Time.time < _reloadUntil)
            {
                return false;
            }

            PlayDueReloadCues();
            _rounds = MagazineSize;
            _reloadUntil = 0f;
            return true;
        }

        /// <summary>Legacy name — prefer <see cref="TryCompleteReloadIfDue"/> when the return value matters.</summary>
        public void ServerCompleteReloadIfDue() => TryCompleteReloadIfDue();

        public float CurrentSpreadDegrees(float horizontalSpeed, float aimDistanceMeters, float exertionPenalty = 0f)
        {
            EnsureInitialized();
            DecayRecoil();
            return AccuracyCone.ComputeSpreadDegrees(_profile, _recoilStacks, horizontalSpeed, aimDistanceMeters, exertionPenalty);
        }

        /// <summary>
        /// World pose for muzzle flash / presentation. Uses the wired socket when present;
        /// otherwise a short offset along the item forward so flash still reads without a recipe re-run.
        /// </summary>
        public void GetMuzzleWorldPose(out Vector3 position, out Vector3 forward)
        {
            Transform muzzle = Muzzle;
            if (muzzle != null && muzzle != transform)
            {
                position = muzzle.position;
                forward = muzzle.forward.sqrMagnitude > 0.0001f ? muzzle.forward.normalized : transform.forward;
                return;
            }

            forward = transform.forward.sqrMagnitude > 0.0001f ? transform.forward.normalized : Vector3.forward;
            position = transform.position + (forward * 0.35f);
        }

        private bool IsServerAuthority()
        {
            return _networkObject != null && _networkObject.IsServer;
        }

        /// <summary>
        /// Server: mag-in and bolt-release sounds at the moments the reload clip shows them
        /// (<see cref="RifleReloadClipTiming"/>), clamped so a shorter reload still plays both.
        /// </summary>
        private void PlayDueReloadCues()
        {
            if (_reloadUntil <= 0f || _reloadCuesPlayed >= 2 || !IsServerAuthority())
            {
                return;
            }

            float elapsed = Time.time - _reloadStartedAt;
            float duration = Mathf.Max(0.01f, _profile.ReloadSeconds);
            if (_reloadCuesPlayed < 1 && elapsed >= Mathf.Min(RifleReloadClipTiming.MagazineInSeconds, duration))
            {
                _reloadCuesPlayed = 1;
                PlayReloadCue(_profile.VisualStyle == RangedVisualStyle.Ballistic
                    ? CombatAudioTrackIds.ReloadMagazineIn : CombatAudioTrackIds.EnergyReady, 1f);
            }

            if (_reloadCuesPlayed < 2 && elapsed >= Mathf.Min(RifleReloadClipTiming.BoltReleaseSeconds, duration))
            {
                _reloadCuesPlayed = 2;
                if (_profile.VisualStyle == RangedVisualStyle.Ballistic)
                {
                    PlayReloadCue(CombatAudioTrackIds.ReloadCock, 0.95f);
                }
            }
        }

        private void PlayReloadCue(string trackId, float volume)
        {
            AudioSubSystem audio = SubSystems.Get<AudioSubSystem>();
            if (audio == null)
            {
                return;
            }

            audio.PlayAudioSource(AudioType.Sfx, trackId, transform.position, null, false, volume, 1f, 8f, 40f);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _rounds = MagazineSize;
            _lastRecoilDecayTime = Time.time;
            _initialized = true;
        }

        private void DecayRecoil()
        {
            float now = Time.time;
            float dt = now - _lastRecoilDecayTime;
            _lastRecoilDecayTime = now;
            if (dt <= 0f || _recoilStacks <= 0f)
            {
                return;
            }

            if (IsOnCooldown)
            {
                return;
            }

            _recoilStacks = Mathf.Max(0f, _recoilStacks - (dt * Mathf.Max(0f, _profile.RecoilDecayPerSecond)));
        }
    }
}
