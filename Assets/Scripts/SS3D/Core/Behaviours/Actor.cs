using Coimbra.Services;
using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used to optimize all GameObjects, avoid MonoBehaviours
    /// </summary>
    [Tooltip("Used to optimize all GameObjects, avoid MonoBehaviours")]
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

        internal virtual void OnEnable()
        {
            Initialize();

            OnEnabled();
        }

        internal virtual void OnDisable()
        {
            OnDisabled();
        }

        private void Awake()
        {
            ActorLocator.Register(this);

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

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnDestroyed() { }
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
        
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