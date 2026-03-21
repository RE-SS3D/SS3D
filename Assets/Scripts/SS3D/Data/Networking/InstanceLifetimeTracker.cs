using System;
using FishNet.Object;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Tracks the server-side lifetime of a spawned network object so
    /// consumers can react when the instance is destroyed.
    /// Operates on backend-agnostic string keys for the new handle-based asset system.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class InstanceLifetimeTracker : MonoBehaviour
    {
        /// <summary>
        /// Raised once when this tracked instance is destroyed.
        /// </summary>
        internal static event Action<string> OnReleased;

        private string _key;
        private bool _released;

        /// <summary>
        /// Arms the tracker for the specified asset key and resets the one-shot release guard.
        /// </summary>
        /// <param name="key">Backend-agnostic identity of the tracked asset (GUID for Addressables, path for Resources, etc.).</param>
        internal void Initialize(string key)
        {
            _key = key;
            _released = false;
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
