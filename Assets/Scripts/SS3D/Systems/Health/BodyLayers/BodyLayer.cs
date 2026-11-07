using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Bodylayers are not networked object, keep in mind they are server side only.
    /// If you need to query anything from them, you'll have to go through bodypart.
    /// Bodylayers should only exists as a part of Bodypart.
    /// </summary>
    public abstract class BodyLayer
    {
        /// <summary>
        /// Type of this bodylayer, a body part should not have two layers of the same type.
        /// </summary>
        public abstract BodyLayerType LayerType { get; }

        public abstract void Cleanlayer();

        /// <summary>
        /// The body part containing this body layer.
        /// </summary>
        public BodyPart BodyPart { get; set; }

        /// <summary>
        /// Events fired when damages are received on this layer.
        /// </summary>
        public event EventHandler<DamageEventArgs> OnDamageReceivedEvent;

        /// <summary>
        /// Maximum amount of damages the body layer can sustain.
        /// </summary>
        public virtual float MaxDamage => 100f;

        public const float MinDamage = 0f;

        /// <summary>
        /// ScriptableObject containing the base resistances and susceptibilities for this type of body layer.
        /// </summary>
        protected BodyLayerData _bodyLayerData;

        /// <summary>
        /// Quantity of damages sustained on this body layer for each damage type.
        /// </summary>
        protected Dictionary<DamageType, float> _damages;

        /// <summary>
        /// Per-instance multiplicative factors modifying the base damage resistances of this body layer.
        /// Empty most of the time (default factor is 1).
        /// </summary>
        protected Dictionary<DamageType, float> _damageResistanceModifiers;

        /// <summary>
        /// Per-instance multiplicative factors modifying the base damage susceptibilities of this body layer.
        /// Empty most of the time (default factor is 1).
        /// </summary>
        protected Dictionary<DamageType, float> _damageSusceptibilityModifiers;

        /// <summary>
        /// Read-only view of the sustained damages on this body layer for each damage type.
        /// </summary>
        public IReadOnlyDictionary<DamageType, float> Damages => _damages;

        /// <summary>
        /// Read-only view of the per-instance multiplicative modifiers applied to base damage resistances.
        /// </summary>
        public IReadOnlyDictionary<DamageType, float> DamageResistanceModifiers => _damageResistanceModifiers;

        /// <summary>
        /// Read-only view of the per-instance multiplicative modifiers applied to base damage susceptibilities.
        /// </summary>
        public IReadOnlyDictionary<DamageType, float> DamageSusceptibilityModifiers => _damageSusceptibilityModifiers;

        /// <summary>
        /// Total amount of damages sustained across all damage types on this body layer.
        /// </summary>
        public float TotalDamage => _damages.Values.Sum();

        /// <summary>
        /// Ratio of total sustained damages relative to the maximum damage this body layer can sustain.
        /// </summary>
        public float RelativeDamage => TotalDamage / MaxDamage;

        /// <summary>
        /// Initializes a new body layer with empty damage and modifier dictionaries.
        /// </summary>
        /// <param name="bodyPart">The bodypart this bodylayer belongs to.</param>
        protected BodyLayer(BodyPart bodyPart)
        {
            _damages = new Dictionary<DamageType, float>();
            _damageResistanceModifiers = new Dictionary<DamageType, float>();
            _damageSusceptibilityModifiers = new Dictionary<DamageType, float>();
            BodyPart = bodyPart;
        }

        /// <summary>
        /// Initializes a new body layer with existing damage and modifier dictionaries.
        /// </summary>
        /// <param name="bodyPart">The bodypart this bodylayer belongs to.</param>
        /// <param name="damages">Initial sustained damages per damage type.</param>
        /// <param name="susceptibilityModifiers">Initial multiplicative modifiers for damage susceptibilities.</param>
        /// <param name="resistanceModifiers">Initial multiplicative modifiers for damage resistances.</param>
        protected BodyLayer(BodyPart bodyPart, Dictionary<DamageType, float> damages,
            Dictionary<DamageType, float> susceptibilityModifiers, Dictionary<DamageType, float> resistanceModifiers)
        {
            _damages = damages ?? new Dictionary<DamageType, float>();
            _damageSusceptibilityModifiers = susceptibilityModifiers ?? new Dictionary<DamageType, float>();
            _damageResistanceModifiers = resistanceModifiers ?? new Dictionary<DamageType, float>();
            BodyPart = bodyPart;
        }

        /// <summary>
        /// Add damage without going above max damage for any given type.
        /// Doesn't simply add the amount passed in parameters, first apply susceptibility and resistance.
        /// </summary>
        /// <param name="damage">The type and amount of damage to inflict, before applying any modifiers.</param>
        public virtual void InflictDamage(DamageTypeQuantity damage)
        {
            float modifiedDamage = ApplyResistanceAndSusceptibility(damage);
            if (modifiedDamage > MinDamage)
            {
                float currentDamage = GetDamageTypeQuantity(damage.damageType);
                _damages[damage.damageType] = ClampDamage(currentDamage + modifiedDamage);
            }

            DamageInflicted(new DamageTypeQuantity(damage.damageType, modifiedDamage));
        }

        protected virtual float ClampDamage(float damage)
        {
            return damage > MaxDamage ? MaxDamage : damage;
        }

        /// <summary>
        /// Remove a given quantity of damages of a given type on this bodylayer. Can't remove below the minimum (should usually be zero).
        /// Remove exactly the amount passed in parameter, no modifiers.
        /// </summary>
        /// <param name="damage">Quantity and amount of damage to remove.</param>
        public virtual void HealDamage(DamageTypeQuantity damage)
        {
            if (!_damages.TryGetValue(damage.damageType, out float currentDamage))
            {
                return;
            }

            float newDamage = currentDamage - damage.quantity;
            if (newDamage <= MinDamage)
            {
                _damages.Remove(damage.damageType);
            }
            else
            {
                _damages[damage.damageType] = newDamage;
            }
        }

        /// <summary>
        /// Get the amount of a given damage type on this body layer.
        /// </summary>
        public float GetDamageTypeQuantity(DamageType damageType)
        {
            return _damages.TryGetValue(damageType, out float quantity) ? quantity : MinDamage;
        }

        /// <summary>
        /// Return the susceptibility to a particular kind of damage.
        /// Multiplies the base value stored in the scriptable object by the modifier in the dictionary (or 1 if none).
        /// </summary>
        public float GetDamageTypeSusceptibility(DamageType damageType)
        {
            float baseSusceptibility = _bodyLayerData != null ? _bodyLayerData.GetSusceptibility(damageType) : 1f;
            float modifier = _damageSusceptibilityModifiers.TryGetValue(damageType, out float factor) ? factor : 1f;
            return baseSusceptibility * modifier;
        }

        /// <summary>
        /// Return the damage resistance for a given damage type.
        /// Multiplies the base value stored in the scriptable object by the modifier in the dictionary (or 1 if none).
        /// </summary>
        public float GetDamageResistance(DamageType damageType)
        {
            float baseResistance = _bodyLayerData != null ? _bodyLayerData.GetResistance(damageType) : 0f;
            float modifier = _damageResistanceModifiers.TryGetValue(damageType, out float factor) ? factor : 1f;
            return baseResistance * modifier;
        }

        public virtual bool IsDestroyed()
        {
            return TotalDamage >= MaxDamage;
        }

        /// <summary>
        /// Modify the quantity of damages taken by applying susceptibility first, and by substracting resistance after.
        /// </summary>
        protected float ApplyResistanceAndSusceptibility(DamageTypeQuantity damageTypeQuantity)
        {
            float susceptibility = GetDamageTypeSusceptibility(damageTypeQuantity.damageType);
            float resistance = GetDamageResistance(damageTypeQuantity.damageType);
            float modifiedDamages = damageTypeQuantity.quantity * susceptibility - resistance;
            return modifiedDamages < 0f ? 0f : modifiedDamages;
        }

        protected virtual void DamageInflicted(DamageTypeQuantity damageQuantity)
        {
            DamageEventArgs args = new DamageEventArgs(damageQuantity);
            OnDamageReceivedEvent?.Invoke(this, args);
        }

        /// <summary>
        /// Take another bodylayer and copy its values to this one. Useful when spawning a new bodypart to preserve data.
        /// </summary>
        /// <param name="other">The layer from which we want the values to copy.</param>
        public void CopyLayerValues(BodyLayer other)
        {
            _damages = new Dictionary<DamageType, float>(other._damages);
            _damageResistanceModifiers = new Dictionary<DamageType, float>(other._damageResistanceModifiers);
            _damageSusceptibilityModifiers = new Dictionary<DamageType, float>(other._damageSusceptibilityModifiers);
        }
    }
}
