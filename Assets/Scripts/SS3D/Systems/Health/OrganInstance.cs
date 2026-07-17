using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Asset-backed organ that registers function state with the parent HumanHealthController.
    /// </summary>
    public class OrganInstance : MonoBehaviour
    {
        [SerializeField] private OrganType _type;

        private HumanHealthController _healthController;

        public OrganType Type => _type;

        public float FunctionPercent => _healthController != null
            ? _healthController.GetStoredOrganFunction(_type)
            : 100f;

        public void Configure(OrganType type)
        {
            if (_type == type && _healthController != null)
            {
                return;
            }

            Unregister();
            _type = type;
            Register();
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Register()
        {
            _healthController = GetComponentInParent<HumanHealthController>();
            _healthController?.RegisterOrgan(this);
        }

        private void Unregister()
        {
            _healthController?.UnregisterOrgan(this);
            _healthController = null;
        }
    }
}
