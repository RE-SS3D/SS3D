using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Bodylayers are not networked object, keep in mind they are server side only.
    /// If you need to query anything from them, you'll have to go through bodypart.
    /// Bodylayers should only exists as a part of Bodypart.
    /// TODO : put the default values for resistance and susceptibilities in scriptable objects, for each type of layer.
    /// </summary>
    public abstract class BodyLayer
    {
        /// <summary>
        /// Events fired when damages are received on this layer.
        /// </summary>
        public event EventHandler<DamageEventArgs> OnDamageReceivedEvent;

        public const float MinDamage = 0;

        /// <summary>
        /// Contains everything regarding resistance, susceptibility and quantity of damages for
        /// each type of damages.
        /// </summary>
        public readonly DamagesContainer Damages = new();

        /// <summary>
        /// TODO : Put default damage suceptibility and resistance into a scriptable object and replace those lists with "damage * modifier".
        /// They should be empty most of the time as they are modifiers. This will improve memory usage.
        /// </summary>
        /// <param name="bodyPart">The bodypart this bodylayer belongs to.</param>
        protected BodyLayer(BodyPart bodyPart)
        {
            SetDamagesContainer();
            BodyPart = bodyPart;
        }

        protected BodyLayer(BodyPart bodyPart, DamagesContainer damages)
        {
            BodyPart = bodyPart;
        }

        /// <summary>
        /// Type of this bodylayer, a body part should not have two layers of the same type.
        /// </summary>
        public abstract BodyLayerType LayerType { get; }

        /// <summary>
        /// The body part containing this body layer.
        /// </summary>
        public BodyPart BodyPart { get; set; }

        /// <summary>
        /// Maximum amount of damages the body layer can sustain.
        /// </summary>
        public virtual float MaxDamage => 100;

        public float TotalDamage => Damages.DamagesInfo.Sum(x => x.Value.Quantity);

        public float RelativeDamage => TotalDamage / MaxDamage;

        public abstract void Cleanlayer();

        /// <summary>
        /// Add damage without going above max damage for any given type.
        /// Doesn't simply add the amount passed in parameters, first apply susceptibility and resistance.
        /// </summary>
        /// <param name="damageToInflict">The type and amount of damage to inflict, before applying any modifiers.</param>
        public virtual void InflictDamage(DamageTypeQuantity damage)
        {
            Damages[damage.DamageType] += damage.Quantity;
            DamageInflicted(damage);
        }

        /// <summary>
        /// Remove a given quantity of damages of a given type on this bodylayer. Can't remove below the minimum (should usually be zero).
        /// Remove exactly the amount passed in parameter, no modifiers.
        /// </summary>
        /// <param name="damage">Quantity and amount of damage to remove.</param>
        public virtual void HealDamage(DamageTypeQuantity damage)
        {
            Damages[damage.DamageType] -= damage.Quantity;
        }

        /// <summary>
        /// Get the amount of a given damage type on this body layer.
        /// </summary>
        public float GetDamageTypeQuantity(DamageType damageType) => Damages[damageType].Quantity;

        /// <summary>
        /// Return the susceptibility to a particular kind of damage. Susceptibility is one if no modifiers.
        /// </summary>
        public float GetDamageTypeSusceptibility(DamageType damageType) => Damages[damageType].Suceptibility;

        /// <summary>
        /// Return the damage resistance for a given damage type.
        /// If no resistance is found, the default resistance is 0.
        /// </summary>
        public float GetDamageResistance(DamageType damageType) => Damages[damageType].Resistance;

        public virtual bool IsDestroyed() => TotalDamage >= MaxDamage;

        /// <summary>
        /// Take another bodylayer and copy its values to this one. Useful when spawning a new bodypart to preserve data.
        /// </summary>
        /// <param name="layer"> The layer from which we want the values to copy.</param>
        public void CopyLayerValues(BodyLayer other)
        {
            foreach (KeyValuePair<DamageType, BodyDamageInfo> x in other.Damages.DamagesInfo)
            {
                Damages.DamagesInfo[x.Key] = new BodyDamageInfo(x.Value.InjuryType, x.Value.Quantity, x.Value.Suceptibility, x.Value.Resistance);
            }
        }

        protected virtual float ClampDamage(float damage) => damage > MaxDamage ? MaxDamage : damage;

        /// <summary>
        /// Modify the quantity of damages taken by applying susceptibility first, and by substracting resistance after.
        /// </summary>
        protected float ApplyResistanceAndSusceptibility(DamageTypeQuantity damageTypeQuantity)
        {
            float susceptibility = GetDamageTypeSusceptibility(damageTypeQuantity.DamageType);
            float resistance = GetDamageResistance(damageTypeQuantity.DamageType);
            float modifiedDamages = (damageTypeQuantity.Quantity * susceptibility) - resistance;
            return modifiedDamages < 0 ? 0 : modifiedDamages;
        }

        protected virtual void DamageInflicted(DamageTypeQuantity damageQuantity)
        {
            DamageEventArgs args = new DamageEventArgs(damageQuantity);
            OnDamageReceivedEvent?.Invoke(this, args);
        }

        /// <summary>
        /// Set all resistances on this body layer.
        /// </summary>
        protected abstract void SetDamagesContainer();
    }
}
