using Coimbra.Services;
using Coimbra.Services.Events;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Actors are the representation of a GameObject with extra steps. The basic idea is to optimize Transform and GameObject manipulation,
    /// as Unity's getters are a bit slow since they do not cache the Transform and the GameObject. They also used for QOL on code usages,
    /// as the Unity doesn't provide some of them from the get-go.
    /// </summary>
    /// 
    /// <remarks>
    /// They will also be used for optimization with the Update calls, as Unity's method is slow and the UpdateEvent event solves that issue and guarantees performance.
    /// Follow this link to read more about PlayerLoopTiming events.
    ///
    /// https://github.com/coimbrastudios/framework/blob/master/Documentation~/EventService.md
    /// </remarks>
    // TODO: Add a Guide into using PlayerLoopTiming events to GitBook.
    public class NetworkActor : NetworkBehaviour, IActor
    {
        /// <summary>
        /// Internal cached game object.
        /// </summary>
        private GameObject _gameObjectCache;

        /// <summary>
        /// Internal cached transform.
        /// </summary>
        private Transform _transformCache;

        /// <summary>
        /// If this Actor is initialized or not.
        /// </summary>
        private bool _actorInitialized;

        /// <summary>
        /// The event bus listeners added to this object, cleared on OnDestroy
        /// </summary>
        private readonly List<EventHandle> _eventHandles = new();

        /// <inheritdoc />
        public int Id => GameObject.GetInstanceID();

        /// <inheritdoc />
        public Transform Transform => GetTransform();
        /// <inheritdoc />
        public GameObject GameObject => GetGameObject();
        /// <inheritdoc />
        public RectTransform RectTransform => (RectTransform)Transform;

        /// <inheritdoc/>
        public bool ActiveSelf => GameObject.activeSelf;
        /// <inheritdoc/>
        public bool ActiveInHierarchy => GameObject.activeInHierarchy;

        /// <inheritdoc/>
        public Transform Parent => Transform.parent;

        /// <inheritdoc/>
        public Vector3 Forward => Transform.forward;

        /// <inheritdoc/>
        public Vector3 Backward => -Transform.forward;

        /// <inheritdoc/>
        public Vector3 Right => Transform.right;

        /// <inheritdoc/>
        public Vector3 Left => -Transform.right;

        /// <inheritdoc/>
        public Vector3 Up => Transform.up;

        /// <inheritdoc/>
        public Vector3 Down => -Transform.up;

        /// <inheritdoc/>
        public Transform Root => Transform.root;

        /// <inheritdoc/>
        public Vector3 Scale => Transform.lossyScale;

        /// <inheritdoc/>
        public Matrix4x4 LocalToWorldMatrix => Transform.localToWorldMatrix;

        /// <inheritdoc/>
        public Matrix4x4 WorldToLocalMatrix => Transform.worldToLocalMatrix;

        /// <inheritdoc/>
        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }

        /// <inheritdoc/>
        public Vector3 LocalPosition
        {
            get => Transform.localPosition;
            set => Transform.localPosition = value;
        }

        /// <inheritdoc/>
        public Vector3 RotationEuler
        {
            get => Transform.eulerAngles;
            set => Transform.eulerAngles = value;
        }

        /// <inheritdoc/>
        public Quaternion Rotation
        {
            get => Transform.rotation;
            set => Transform.rotation = value;
        }

        /// <inheritdoc/>
        public Quaternion LocalRotation
        {
            get => Transform.localRotation;
            set => Transform.localRotation = value;
        }

        /// <inheritdoc/>
        public Vector3 LocalEuler
        {
            get => Transform.localEulerAngles;
            set => Transform.localEulerAngles = value;
        }

        /// <inheritdoc/>
        public Vector3 LocalScale
        {
            get => Transform.localScale;
            set => Transform.localScale = value;
        }

        /// <inheritdoc/>
        public void SetActive(bool state)
        {
            GameObject.SetActive(state);
        }

        /// <inheritdoc/>
        public void SetParent(Transform parent)
        {
            Transform.SetParent(parent);
        }

        /// <inheritdoc/>
        public void LookAt(Transform target)
        {
            Transform.LookAt(target);
        }

        /// <inheritdoc/>
        public void LookAt(Vector3 target)
        {
            Transform.LookAt(target);
        }

        /// <inheritdoc/>
        public void AddHandle(EventHandle handle)
        {
            _eventHandles.Add(handle);
        }

        protected void OnEnable()
        {
            OnEnabled();
        }

        protected void OnDisable()
        {
            OnDisabled();
        }

        protected void Awake()
        {
            Initialize();

            OnAwake();
        }

        protected void Start()
        {
            OnStart();
        }

        protected void OnDestroy()
        {
            RemoveEventListeners();
            OnDestroyed();
        }

        /// <summary>
        /// Called once the script is loaded and registered on ActorLocator.
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Called once the start function is called.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Called once the Actor is destroyed, after removing the Actor from the ActorLocator and all event listeners.
        /// </summary>
        protected virtual void OnDestroyed() { }

        /// <summary>
        /// Called when the Actor's GameObject is enabled. 
        /// </summary>
        protected virtual void OnEnabled() { }

        /// <summary>
        /// Called when the Actor's GameObject is disabled.
        /// </summary>
        protected virtual void OnDisabled() { }

        /// <summary>
        /// Initializes the Actor, caching the Transform and the GameObject.
        /// </summary>
        private void Initialize()
        {
            _transformCache = transform;
            _gameObjectCache = gameObject;

            _actorInitialized = true;
        }

        /// <summary>
        /// Removes all subscribed event listeners, to avoid null refs and CPU usage.
        /// </summary>
        private void RemoveEventListeners()
        {
            IEventService eventService = ServiceLocator.GetChecked<IEventService>();

            foreach (EventHandle eventHandle in _eventHandles)
            {
                eventService.RemoveListener(eventHandle);
            }
        }

        /// <summary>
        /// Tries to get the Transform, and if the Actor is not initialized we initialize it.
        /// </summary>
        /// <returns>Returns the Cached Transform</returns>
        private Transform GetTransform()
        {
            if (!_actorInitialized)
            {
                Initialize();
            }

            return _transformCache;
        }

        /// <summary>
        /// Tries to get the GameObject, and if the Actor is not initialized we initialize it.
        /// </summary>
        /// <returns>Returns the Cached Game Object</returns>
        private GameObject GetGameObject()
        {
            if (!_actorInitialized)
            {
                Initialize();
            }

            return _gameObjectCache;
        }
    }
}