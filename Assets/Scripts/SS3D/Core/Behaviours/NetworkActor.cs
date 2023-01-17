using System.Collections.Generic;
using Coimbra.Services;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using UnityEngine;

// ReSharper disable InconsistentNaming
namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used to optimize all NetworkObjects, avoid MonoBehaviours
    /// </summary>
    [Tooltip("Used to optimize all GameObjects, avoid MonoBehaviours")]
    public class NetworkActor : NetworkBehaviour
    {
        private GameObject _gameObjectCache;
        private Transform _transformCache;

        private bool _initialized;

        private readonly List<EventHandle> _eventHandles = new();

        public Transform TransformCache
        {
            get
            {
                if (!_initialized)
                {
                    _transformCache = transform;
                }

                return _transformCache;
            }
            private set => _transformCache = value;
        }

        public GameObject GameObjectCache
        {
            get
            {
                if (!_initialized)
                {
                    _gameObjectCache = gameObject;

                }

                return _gameObjectCache;
            }
            private set => _gameObjectCache = value;
        }

        #region ACCESSORS
        public RectTransform RectTransform => (RectTransform)TransformCache;

        public Vector3 Position
        {
            get => TransformCache.position;
            set => TransformCache.position = value;
        }

        public Vector3 RotationEuler
        {
            get => TransformCache.eulerAngles;
            set => TransformCache.eulerAngles = value;
        }

        public Quaternion Rotation
        {
            get => TransformCache.rotation;
            set => TransformCache.rotation = value;
        }

        public bool ActiveSelf => GameObjectCache.activeSelf;
        public bool ActiveInHierarchy => GameObjectCache.activeInHierarchy;

        public Transform Parent => TransformCache.parent;

        public Vector3 Forward => TransformCache.forward;
        public Vector3 Backward => -TransformCache.forward;
        public Vector3 Right => TransformCache.right;
        public Vector3 Left => -TransformCache.right;
        public Vector3 Up => TransformCache.up;
        public Vector3 Down => -TransformCache.up;

        public Transform Root => TransformCache.root;

        public void SetActive(bool state) => GameObjectCache.SetActive(state);
        public void SetParent(Transform parent) => TransformCache.SetParent(parent);
        public void LookAt(Transform target) => TransformCache.LookAt(target);
        public void LookAt(Vector3 target) => TransformCache.LookAt(target);

        public void AddHandle(EventHandle handle) => _eventHandles.Add(handle);

        public Vector3 LocalPosition
        {
            get => TransformCache.localPosition;
            set => TransformCache.localPosition = value;
        }

        public Quaternion LocalRotation
        {
            get => TransformCache.localRotation;
            set => TransformCache.localRotation = value;
        }

        public Vector3 LocalEuler
        {
            get => TransformCache.localEulerAngles;
            set => TransformCache.localEulerAngles = value;
        }

        public Vector3 LocalScale
        {
            get => TransformCache.localScale;
            set => TransformCache.localScale = value;
        }

        public Vector3 Scale => TransformCache.lossyScale;

        public Matrix4x4 LocalToWorldMatrix => TransformCache.localToWorldMatrix;
        public Matrix4x4 WorldToLocalMatrix => TransformCache.worldToLocalMatrix;
        #endregion

        #region SETUP
        private void Awake()
        {
            Initialize();
            OnAwake();
        }

        private void Start()
        {
            AddEventListeners();
            OnStart();
        }

        private void OnDestroy()
        {
            OnDestroyed();
        }

        private void Initialize()
        {
            TransformCache = transform;
            GameObjectCache = gameObject;

            _initialized = true;
        }

        private void AddEventListeners()
        {
            _eventHandles.Add(LastPreUpdateEvent.AddListener(OnPreUpdate));
            _eventHandles.Add(UpdateEvent.AddListener(OnUpdate));
            _eventHandles.Add(LateUpdateEvent.AddListener(OnLateUpdate));
        }

        private void RemoveEventListeners()
        {
            IEventService eventService = ServiceLocator.Get<IEventService>();
            foreach (EventHandle eventHandle in _eventHandles)
            {
                eventService?.RemoveListener(eventHandle);
            }
        }

        #endregion

        #region EVENT_CALLS
        private void OnPreUpdate(ref EventContext context, in LastPreUpdateEvent e) { HandlePreUpdate(e.DeltaTime); }
        private void OnUpdate(ref EventContext context, in UpdateEvent e) { HandleUpdate(e.DeltaTime); }
        private void OnLateUpdate(ref EventContext context, in LateUpdateEvent e) { HandleLateUpdate(e.DeltaTime); }

        #endregion

        #region EVENT_CALLBACKS
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }

        protected virtual void OnDestroyed()
        {
            RemoveEventListeners();
        }

        protected virtual void HandlePreUpdate(in float deltaTime) { }
        protected virtual void HandleLateUpdate(float deltaTime) { }
        protected virtual void HandleUpdate(in float deltaTime) { }
        #endregion
    }
}