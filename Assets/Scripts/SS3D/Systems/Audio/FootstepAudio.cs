using System.Linq;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Logging;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Client-local footwear footsteps (audio.md §3 presentation). The Socks/Shoes/Boots clips are
    /// ~2s walking loops — this plays them on a local <see cref="AudioSource"/> while the body is
    /// moving. Does <b>not</b> go through <see cref="AudioSubSystem"/>: host prediction clears
    /// server locomotion every tick with <c>Move(default)</c>, so a server-pooled loop never stayed
    /// audible. Owner uses predicted locomotion velocity; remotes use transform delta.
    /// Added from <c>OnStartNetwork</c> — do not edit Human.prefab.
    /// </summary>
    public sealed class FootstepAudio : MonoBehaviour
    {
        private const float OwnerSpeedThreshold = 0.05f;
        private const float RemoteSpeedThreshold = 0.35f;
        private const float Volume = 0.45f;
        private const float SpatialBlend = 1f;
        private const float MinDistance = 1f;
        private const float MaxDistance = 12f;

        private HumanoidController _controller;
        private HumanoidBodyStateMachine _bodyStateMachine;
        private HumanInventory _inventory;
        private Ragdoll _ragdoll;
        private AudioSource _source;
        private AudioSourceOcclusion _occlusion;

        private float _ownerSpeed;
        private Vector3 _lastPosition;
        private bool _hasLastPosition;
        private string _loadedTrackId = string.Empty;
        private bool _loggedMissingClip;

        private void Awake()
        {
            _controller = GetComponent<HumanoidController>();
            _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            _inventory = GetComponent<HumanInventory>();
            _ragdoll = GetComponent<Ragdoll>();

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = SpatialBlend;
            _source.minDistance = MinDistance;
            _source.maxDistance = MaxDistance;
            _source.rolloffMode = AudioRolloffMode.Logarithmic;
            _source.dopplerLevel = 0f;
            _source.volume = Volume;
            _source.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.SfxGroup);
            _occlusion = gameObject.AddComponent<AudioSourceOcclusion>();
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.OnLocomotionVelocityChanged += HandleLocomotionVelocityChanged;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnLocomotionVelocityChanged -= HandleLocomotionVelocityChanged;
            }

            StopFootsteps();
        }

        private void HandleLocomotionVelocityChanged(float velX, float velZ, float turn)
        {
            _ownerSpeed = Mathf.Sqrt((velX * velX) + (velZ * velZ));
        }

        private void Update()
        {
            if (_ragdoll != null && _ragdoll.IsKnockedDown)
            {
                StopFootsteps();
                return;
            }

            if (_bodyStateMachine != null && _bodyStateMachine.Snapshot.IsFloating)
            {
                StopFootsteps();
                return;
            }

            bool moving = IsMoving();
            if (!moving)
            {
                StopFootsteps();
                return;
            }

            string trackId = ResolveTrackId();
            if (!EnsureClip(trackId))
            {
                return;
            }

            if (!_source.isPlaying)
            {
                _occlusion.PrepareForPlayback(Volume);
                _source.time = 0f;
                _source.Play();
            }
        }

        private bool IsMoving()
        {
            // Owner: predicted locomotion axes are published every move tick.
            if (_controller != null && _controller.IsOwner)
            {
                return _ownerSpeed >= OwnerSpeedThreshold;
            }

            // Remotes: no predicted velocity — approximate from world motion.
            Vector3 position = transform.position;
            if (!_hasLastPosition)
            {
                _lastPosition = position;
                _hasLastPosition = true;
                return false;
            }

            float dt = Time.deltaTime;
            float speed = dt > 0f ? (position - _lastPosition).magnitude / dt : 0f;
            _lastPosition = position;
            return speed >= RemoteSpeedThreshold;
        }

        private bool EnsureClip(string trackId)
        {
            if (_loadedTrackId == trackId && _source.clip != null)
            {
                return true;
            }

            if (!Assets.TryGet(AssetDatabases.Sounds, trackId, out AudioClip clip) || clip == null)
            {
                if (!_loggedMissingClip)
                {
                    _loggedMissingClip = true;
                    Log.Warning(this,
                        "Footstep clip '{id}' missing from Sounds database — open the Footsteps mp3s in Unity so they import, then confirm Sounds.asset refs are not Missing.",
                        Logs.Generic, trackId);
                }

                return false;
            }

            _source.clip = clip;
            _loadedTrackId = trackId;
            if (_source.isPlaying)
            {
                _source.Stop();
                _source.Play();
            }

            return true;
        }

        private void StopFootsteps()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }

        private string ResolveTrackId()
        {
            Item footwear = TryGetFootwearItem();
            if (footwear == null)
            {
                return FootstepAudioTrackIds.Socks;
            }

            string name = footwear.Name;
            if (name.IndexOf("boot", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return FootstepAudioTrackIds.Boots;
            }

            return FootstepAudioTrackIds.Shoes;
        }

        private Item TryGetFootwearItem()
        {
            if (_inventory == null)
            {
                return null;
            }

            if (TryFirstItem(ContainerType.ShoeLeft, out Item left))
            {
                return left;
            }

            if (TryFirstItem(ContainerType.ShoeRight, out Item right))
            {
                return right;
            }

            return null;
        }

        private bool TryFirstItem(ContainerType type, out Item item)
        {
            item = null;
            if (!_inventory.TryGetTypeContainer(type, 0, out AttachedContainer container) || container == null)
            {
                return false;
            }

            item = container.Items.FirstOrDefault();
            return item != null;
        }
    }
}
