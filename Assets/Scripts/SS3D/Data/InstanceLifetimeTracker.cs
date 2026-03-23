using System;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Tracks the lifetime of a spawned asset instance so the asset system
    /// can react when the instance is created or destroyed.
    /// <para>
    /// Uses <see cref="ISerializationCallbackReceiver"/> to detect
    /// <see cref="UnityEngine.Object.Instantiate(Object)"/> copies — works for
    /// both active and inactive GameObjects.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class InstanceLifetimeTracker : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when a new instance is created via <see cref="UnityEngine.Object.Instantiate(Object)"/>
        /// (detected by <see cref="OnAfterDeserialize"/>) or via manual
        /// <see cref="Initialize"/> on a scene instance.
        /// </summary>
        internal static event Action<string> OnInstantiated;

        /// <summary>
        /// Raised once when this tracked instance is destroyed.
        /// </summary>
        internal static event Action<string> OnReleased;

        [SerializeField]
        private string _key;

        [NonSerialized]
        private bool _announced;

        private bool _released;

        /// <summary>
        /// Arms the tracker for the specified asset key and resets the one-shot release guard.
        /// For manual <c>AddComponent</c> on scene instances (e.g. pre-instantiated NetworkObject path).
        /// Does NOT fire <see cref="OnInstantiated"/> on loaded-but-not-instantiated prefabs
        /// because their scene is not valid.
        /// </summary>
        internal void Initialize(string key)
        {
            _key = key;
            _released = false;

            if (!_announced && gameObject.scene.IsValid())
            {
                _announced = true;
                OnInstantiated?.Invoke(_key);
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (_announced || string.IsNullOrEmpty(_key))
            {
                return;
            }

            _announced = true;
            OnInstantiated?.Invoke(_key);
        }

        private void OnDestroy()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            OnReleased?.Invoke(_key);
        }
    }
}