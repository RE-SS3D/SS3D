using Coimbra.Services;
using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Actors are the representation of a GameObject with extra steps. The basic idea is to optimize Transform and GameObject manipulation,
    /// as Unity's getters are a bit slow since they do not cache the Transform and the GameObject. They also used for QOL on code usages,
    /// as the Unity doesn't provide some of them from the get-go.
    ///
    /// They will also be used for optimization with the Update calls, as Unity's method is slow and the UpdateEvent event solves that issue and guarantees performance.
    /// </summary>
    public class Actor : ActorBase, IActor
    {
        public int Id => GameObject.GetInstanceID();

        public Transform Transform => GetTransform();
        public GameObject GameObject => GetGameObject();
        public RectTransform RectTransform => (RectTransform)Transform;

        public bool ActiveSelf => GameObject.activeSelf;
        public bool ActiveInHierarchy => GameObject.activeInHierarchy;

        public Transform Parent => Transform.parent;

        public Vector3 Forward => Transform.forward;
        public Vector3 Backward => -Transform.forward;
        public Vector3 Right => Transform.right;
        public Vector3 Left => -Transform.right;
        public Vector3 Up => Transform.up;
        public Vector3 Down => -Transform.up;

        public Transform Root => Transform.root;
        public Vector3 Scale => Transform.lossyScale;

        public Matrix4x4 LocalToWorldMatrix => Transform.localToWorldMatrix;
        public Matrix4x4 WorldToLocalMatrix => Transform.worldToLocalMatrix;

        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }

        public Vector3 LocalPosition
        {
            get => Transform.localPosition;
            set => Transform.localPosition = value;
        }
        public Vector3 RotationEuler
        {
            get => Transform.eulerAngles;
            set => Transform.eulerAngles = value;
        }

        public Quaternion Rotation
        {
            get => Transform.rotation;
            set => Transform.rotation = value;
        }

        public Quaternion LocalRotation
        {
            get => Transform.localRotation;
            set => Transform.localRotation = value;
        }

        public Vector3 LocalEuler
        {
            get => Transform.localEulerAngles;
            set => Transform.localEulerAngles = value;
        }

        public Vector3 LocalScale
        {
            get => Transform.localScale;
            set => Transform.localScale = value;
        }

        public void SetActive(bool state) => GameObject.SetActive(state);
        public void SetParent(Transform parent) => Transform.SetParent(parent);

        public void LookAt(Transform target) => Transform.LookAt(target);
        public void LookAt(Vector3 target) => Transform.LookAt(target);

        public void AddHandle(EventHandle handle) => EventHandles.Add(handle);

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

        internal virtual void OnEnable()
        {
            OnEnabled();
        }

        internal virtual void OnDisable()
        {
            OnDisabled();
        }

        private void Awake()
        {
            Initialize();

            OnAwake();
        }

        private void Start()
        {
            OnStart();
        }

        private void OnDestroy()
        {
            ActorLocator.Unregister(this);
            RemoveEventListeners();

            OnDestroyed();
        }

        private void Initialize()
        {
            TransformCache = transform;
            GameObjectCache = gameObject;

            Initialized = true;

            ActorLocator.Register(this);
        }

        private void RemoveEventListeners()
        {
            IEventService eventService = ServiceLocator.Get<IEventService>();

            foreach (EventHandle eventHandle in EventHandles)
            {
                eventService?.RemoveListener(eventHandle);
            }
        }

        private Transform GetTransform()
        {
            if (!Initialized)
            {
                Initialize();
            }

            return TransformCache;
        }

        private GameObject GetGameObject()
        {
            if (!Initialized)
            {
                Initialize();
            }

            return GameObjectCache;
        }
    }
}