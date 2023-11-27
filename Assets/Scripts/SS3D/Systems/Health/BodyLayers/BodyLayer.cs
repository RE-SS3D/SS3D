using System.Collections.Generic;
using System.Linq;
using System;

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
		public virtual float MaxDamage => 100;

		public const float MinDamage = 0;

		public float TotalDamage => Damages.DamagesInfo.Sum(x => x.Value.Quantity);

        public float RelativeDamage => TotalDamage/MaxDamage;

        /// <summary>
        /// Contains everything regarding resistance, susceptibility and quantity of damages for
        /// each type of damages.
        /// </summary>
        public DamagesContainer Damages = new();

		/// <summary>
		/// TODO : Put default damage suceptibility and resistance into a scriptable object and replace those lists with "damage * modifier".
		/// They should be empty most of the time as they are modifiers. This will improve memory usage.
		/// </summary>
		/// <param name="bodyPart">The bodypart this bodylayer belongs to.</param>
		public BodyLayer(BodyPart bodyPart)
		{
            SetDamagesContainer();
			BodyPart = bodyPart;    
		}

		public BodyLayer(BodyPart bodyPart, DamagesContainer damages)
		{

			BodyPart = bodyPart;
		}
        
		/// <summary>
		/// Add damage without going above max damage for any given type. 
        /// Doesn't simply add the amount passed in parameters, first apply susceptibility and resistance.
		/// </summary>
		/// <param name="damageToInflict">The type and amount of damage to inflict, before applying any modifiers.</param>
		public virtual void InflictDamage(DamageTypeQuantity damage)
		{
            Damages[damage.damageType] += damage.quantity;

			DamageInflicted(damage);
		}

        protected virtual float ClampDamage(float damage)
        {
            return damage > MaxDamage? MaxDamage : damage;
        }

        /// <summary>
        /// Remove a given quantity of damages of a given type on this bodylayer. Can't remove below the minimum (should usually be zero).
        /// Remove exactly the amount passed in parameter, no modifiers.
        /// </summary>
        /// <param name="damage">Quantity and amount of damage to remove.</param>
		public virtual void HealDamage(DamageTypeQuantity damage)
		{
            Damages[damage.damageType] -= damage.quantity;
        }

		/// <summary>
		/// Get the amount of a given damage type on this body layer.
		/// </summary>
		public float GetDamageTypeQuantity(DamageType damageType)
		{
            return Damages[damageType].Quantity;
        }

		/// <summary>
		/// Return the susceptibility to a particular kind of damage. Susceptibility is one if no modifiers.
		/// </summary>
		public float GetDamageTypeSusceptibility(DamageType damageType)
		{
            return Damages[damageType].Suceptibility;
        }

		/// <summary>
		/// Return the damage resistance for a given damage type.
		/// If no resistance is found, the default resistance is 0.
		/// </summary>
		public float GetDamageResistance(DamageType damageType)
		{
            return Damages[damageType].Resistance;
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
			return modifiedDamages < 0 ? 0 : modifiedDamages;
		}

		protected virtual void DamageInflicted(DamageTypeQuantity damageQuantity)
		{
			DamageEventArgs args = new DamageEventArgs(damageQuantity);
			OnDamageReceivedEvent?.Invoke(this, args);
		}

        /// <summary>
        /// Take another bodylayer and copy its values to this one. Useful when spawning a new bodypart to preserve data.
        /// </summary>
        /// <param name="layer"> The layer from which we want the values to copy.</param>
		public void CopyLayerValues(BodyLayer other)
		{
            foreach(KeyValuePair<DamageType, BodyDamageInfo> x in other.Damages.DamagesInfo)
            {
                Damages.DamagesInfo[x.Key] = new BodyDamageInfo(x.Value.InjuryType, x.Value.Quantity,
                    x.Value.Suceptibility, x.Value.Resistance);
            }
		}

        /// <summary>
        /// Set all resistances on this body layer.
        /// </summary>
        protected abstract void SetDamagesContainer();
	}
}
