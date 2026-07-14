using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Asset-backed organ that registers function state with the parent HumanHealthController.
    /// </summary>
    public class OrganInstance : MonoBehaviour
    {
        [SerializeField] private OrganType _type;
        [SerializeField] private float _functionPercent = 100f;

        public OrganType Type => _type;
        public float FunctionPercent => _functionPercent;

        private HumanHealthController _healthController;

        private void OnEnable()
        {
            _healthController = GetComponentInParent<HumanHealthController>();
            _healthController?.RegisterOrgan(this);
        }

        private void OnDisable()
        {
            _healthController?.UnregisterOrgan(this);
        }
    }
}
