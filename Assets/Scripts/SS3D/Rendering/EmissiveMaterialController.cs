using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Rendering
{
    public class EmissiveMaterialController : Actor
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Range(0, 2f)]
        [SerializeField]
        private float _emissionIntensity;

        [SerializeField]
        private Color _emissionColorValue;

        [SerializeField]
        private Material _originalMaterial;

        [SerializeField]
        private Renderer _renderer;

        private Material _materialInstance;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            Setup();
        }

        protected void OnValidate()
        {
            UpdateVisuals();
        }

        private void Setup()
        {
            _materialInstance = new Material(_originalMaterial);
            _renderer.material = _materialInstance;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_materialInstance == null)
            {
                Setup();
            }

            _materialInstance.SetVector(EmissionColor, _emissionColorValue * _emissionIntensity);
        }
    }
}