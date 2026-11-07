using Coimbra;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// ScriptableObject storing base damage resistances and susceptibilities for a type of body layer.
    /// </summary>
    [CreateAssetMenu(fileName = "BodyLayerData", menuName = "SS3D/Health/BodyLayerData")]
    public class BodyLayerData : ScriptableObject
    {
        /// <summary>
        /// Minimum amount of damage needed before a given damage type hurts this body layer.
        /// Default resistance is 0 when not specified.
        /// </summary>
        [SerializeField]
        private SerializableDictionary<DamageType, float> _damageResistances = new();

        /// <summary>
        /// Multiplier applied to incoming damages of a given type on this body layer.
        /// Default susceptibility is 1 when not specified.
        /// </summary>
        [SerializeField]
        private SerializableDictionary<DamageType, float> _damageSusceptibilities = new();

        /// <summary>
        /// Read-only access to the base damage resistances dictionary.
        /// </summary>
        public SerializableDictionary<DamageType, float> DamageResistances => _damageResistances;

        /// <summary>
        /// Read-only access to the base damage susceptibilities dictionary.
        /// </summary>
        public SerializableDictionary<DamageType, float> DamageSusceptibilities => _damageSusceptibilities;

        /// <summary>
        /// Return the base damage resistance for a given damage type.
        /// If no resistance is found, the default resistance is 0.
        /// </summary>
        public float GetResistance(DamageType damageType)
        {
            return _damageResistances.TryGetValue(damageType, out float resistance) ? resistance : 0f;
        }

        /// <summary>
        /// Return the base susceptibility to a particular kind of damage.
        /// If no susceptibility is found, the default susceptibility is 1.
        /// </summary>
        public float GetSusceptibility(DamageType damageType)
        {
            return _damageSusceptibilities.TryGetValue(damageType, out float susceptibility) ? susceptibility : 1f;
        }
    }
}
