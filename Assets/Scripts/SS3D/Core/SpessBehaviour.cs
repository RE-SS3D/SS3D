using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using UnityEngine;

// ReSharper disable InconsistentNaming
namespace SS3D.Core
{
    /// <summary>
    /// Used to optimize all GameObjects, avoid MonoBehaviours
    /// </summary>
    [Tooltip("Used to optimize all GameObjects, avoid MonoBehaviours")]
    public class SpessBehaviour : NetworkBehaviour
    {
        public Transform Transform { get; private set; }
        public GameObject GameObject { get; private set; }

        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }

        public Vector3 RotationEuler
        {
            get => Transform.rotation.eulerAngles;
            set => Transform.rotation = Quaternion.Euler(value);
        }

        public Quaternion Rotation
        {
            get => Transform.rotation;
            set => Transform.rotation = value;
        }

        public RectTransform RectTransform => (RectTransform)Transform;


        private void Awake()
        {
            Initialize();
            ListenToEvents();
            OnAwake();
        }
        
        private void Start()
        {
            OnStart();
        }

        private void Initialize()
        {
            Transform = base.transform;
            GameObject = gameObject;
        }

        private void ListenToEvents()
        {
            LastPreUpdateEvent.AddListener(OnPreUpdate);
            UpdateEvent.AddListener(OnUpdate);
            LateUpdateEvent.AddListener(OnLateUpdate);
        }

        private void OnPreUpdate(ref EventContext context, in LastPreUpdateEvent e) { HandlePreUpdate(e.DeltaTime); }
        private void OnUpdate(ref EventContext context, in UpdateEvent e) { HandleUpdate(e.DeltaTime); }
        private void OnLateUpdate(ref EventContext context, in LateUpdateEvent e) { HandleLateUpdate(e.DeltaTime); }

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }

        protected virtual void HandlePreUpdate(in float deltaTime) { }
        protected virtual void HandleLateUpdate(float deltaTime) { }
        protected virtual void HandleUpdate(in float deltaTime) { }
    }
}