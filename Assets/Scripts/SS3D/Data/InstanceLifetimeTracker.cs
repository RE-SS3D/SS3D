using System;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Tracks the lifetime of a spawned asset instance so the asset system
    /// can react when the instance is created or destroyed.
    /// <para>
    /// Uses <see cref="ISerializationCallbackReceiver"/> to detect
    /// <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/> copies — works for
    /// both active and inactive GameObjects.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AssetIdentifier))]
    internal sealed class InstanceLifetimeTracker : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when a new instance is created via <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>
        /// (detected by <see cref="OnAfterDeserialize"/>) or via manual
        /// <see cref="Initialize"/> on a scene instance.
        /// </summary>
        internal static event Action<string> OnInstantiated;

        /// <summary>
        /// Raised once when this tracked instance is destroyed.
        /// </summary>
        internal static event Action<string> OnReleased;

        [SerializeField]
        [HideInInspector]
        private AssetIdentifier _identifier;

        [SerializeField]
        [HideInInspector]
        private bool _armed;

        [NonSerialized]
        private bool _announced;

        [NonSerialized]
        private string _announcedGuid;

        private bool _released;
        
        internal string Key => _identifier ? _identifier.AssetGuid : null;

        internal bool IsArmed => _armed;

        internal AssetIdentifier Identifier => _identifier;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            // Avoid Unity API checks here. Instantiate can deserialize a clone before its
            // scene is fully assigned, and serialization callbacks are not a safe place
            // for those queries.
            TryAnnounce();
        }

        /// <summary>
        /// Arms the tracker for the specified asset key and resets the one-shot guards.
        /// For manual <c>AddComponent</c> on scene instances (e.g. pre-instantiated NetworkObject path).
        /// Does NOT fire <see cref="OnInstantiated"/> on loaded-but-not-instantiated prefabs because their scene is not valid.
        /// </summary>
        internal void Initialize(string key)
        {
            EnsureIdentifier(key);
            _armed = true;
            _released = false;

            if (_announced || !gameObject.scene.IsValid())
            {
                return;
            }

            Announce();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Wires the prefab asset to its shared <see cref="AssetIdentifier"/> and leaves it disarmed.
        /// Runtime loading arms the already-present component before any clone can announce itself.
        /// </summary>
        internal bool ConfigureForPrefabAsset(AssetIdentifier identifier)
        {
            bool modified = false;

            if (_identifier != identifier)
            {
                _identifier = identifier;
                modified = true;
            }

            if (!_armed)
            {
                return modified;
            }

            _armed = false;

            return true;
        }
#endif

        private void Awake()
        {
            TryAnnounce();
        }

        private void EnsureIdentifier(string key)
        {
            if (!_identifier && !TryGetComponent(out _identifier))
            {
                _identifier = gameObject.AddComponent<AssetIdentifier>();
            }

            if (_identifier && _identifier.AssetGuid != key)
            {
                _identifier.SetGuid(key);
            }
        }

        private void TryAnnounce()
        {
            if (!_armed || _announced || _identifier is null || string.IsNullOrEmpty(_identifier.AssetGuid))
            {
                return;
            }

            Announce();
        }

        private void Announce()
        {
            _announced = true;
            _announcedGuid = _identifier.AssetGuid;
            OnInstantiated?.Invoke(_announcedGuid);
        }

        private void OnDestroy()
        {
            if (_released || !_announced)
            {
                return;
            }

            _released = true;
            OnReleased?.Invoke(_announcedGuid);
        }
    }
}
