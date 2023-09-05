using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using FishNet.Object;

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

		protected List<DamageTypeQuantity> _damageTypeQuantities;

		protected List<DamageTypeQuantity> _damageResistances;

		protected List<DamageTypeQuantity> _damageSuceptibilities;

        /// <summary>
        /// Maximum amount of damages the body layer can sustain.
        /// </summary>
		public virtual float MaxDamage => 100;

		public const float MinDamage = 0;

		public float TotalDamage => DamageTypeQuantities.Sum(x => x.quantity);

        public float RelativeDamage => TotalDamage/MaxDamage;

        /// <summary>
        /// Quantity of damages on this bodyLayer for each type of damages.
        /// </summary>
        public List<DamageTypeQuantity> DamageTypeQuantities => _damageTypeQuantities;

        /// <summary>
        /// Minimum amount of damage to do to make any actual damage for each type of damages.
        /// </summary>
        public List<DamageTypeQuantity> DamageResistances => _damageResistances;

        /// <summary>
        /// Susceptibility to damage, damages are multiplied by this number, for each type of damages.
        /// </summary>
        public List<DamageTypeQuantity> DamageSuceptibilities => _damageSuceptibilities;

		/// <summary>
		/// TODO : Put default damage suceptibility and resistance into a scriptable object and replace those lists with "damage * modifier".
		/// They should be empty most of the time as they are modifiers. This will improve memory usage.
		/// </summary>
		/// <param name="bodyPart">The bodypart this bodylayer belongs to.</param>
		public BodyLayer(BodyPart bodyPart)
		{
			_damageResistances = new List<DamageTypeQuantity>();
			_damageSuceptibilities= new List<DamageTypeQuantity>();
			_damageTypeQuantities = new List<DamageTypeQuantity>();
			SetResistances();
			SetSuceptibilities();
			BodyPart = bodyPart;    
		}

		public BodyLayer(BodyPart bodyPart, List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
		{
			_damageResistances = resistances;
			_damageSuceptibilities = susceptibilities;
			_damageTypeQuantities = damages;
			BodyPart = bodyPart;
		}



		/// <summary>
		/// Add damage without going above max damage for any given type. 
        /// Doesn't simply add the amount passed in parameters, first apply susceptibility and resistance.
		/// </summary>
		/// <param name="damageQuantity">The type and amount of damage to inflict, before applying any modifiers.</param>
		public virtual void InflictDamage(DamageTypeQuantity damageToInflict)
		{
			DamageTypeQuantity damage = (DamageTypeQuantity) damageToInflict.Clone();

			float currentDamageQuantity = GetDamageTypeQuantity(damage.damageType);
			damage.quantity = ApplyResistanceAndSusceptibility(damage);

			if (currentDamageQuantity == MinDamage)
			{
            
				if (damage.quantity > MaxDamage)
				{
					damage.quantity = MaxDamage;
				}
				DamageTypeQuantities.Add(damage);
			}
			else
			{
				int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damage.damageType);

				float newDamageQuantity = damage.quantity + DamageTypeQuantities[damageTypeIndex].quantity;
				if (newDamageQuantity > MaxDamage)
				{
					DamageTypeQuantities[damageTypeIndex].quantity = MaxDamage;
				}
				else
				{
					DamageTypeQuantities[damageTypeIndex].quantity = newDamageQuantity;
				}
			}

			DamageInflicted(damage);
		}

        /// <summary>
        /// Remove a given quantity of damages of a given type on this bodylayer. Can't remove below the minimum (should usually be zero).
        /// Remove exactly the amount passed in parameter, no modifiers.
        /// </summary>
        /// <param name="damage">Quantity and amount of damage to remove.</param>
		public virtual void HealDamage(DamageTypeQuantity damage)
		{
			float currentDamageQuantity = GetDamageTypeQuantity(damage.damageType);
			if (currentDamageQuantity == MinDamage)
			{
				return;
			}
			else
			{
				int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damage.damageType);
				float newDamageQuantity = DamageTypeQuantities[damageTypeIndex].quantity - damage.quantity;
				if (newDamageQuantity < MinDamage)
				{
					DamageTypeQuantities.RemoveAt(damageTypeIndex);
				}
				else
				{
					DamageTypeQuantities[damageTypeIndex].quantity = newDamageQuantity;
				}
			}
		}

		/// <summary>
		/// Get the amount of a given damage type on this body layer.
		/// </summary>
		public float GetDamageTypeQuantity(DamageType damageType)
		{
			int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damageType);
			return damageTypeIndex == -1 ? MinDamage : DamageTypeQuantities[damageTypeIndex].quantity;
		}

		/// <summary>
		/// Return the susceptibility to a particular kind of damage. Susceptibility is one if no modifiers.
		/// </summary>
		public float GetDamageTypeSusceptibility(DamageType damageType)
		{
			int damageTypeIndex = _damageSuceptibilities.FindIndex(x => x.damageType == damageType);
			return damageTypeIndex == -1 ? 1 : _damageSuceptibilities[damageTypeIndex].quantity;
		}

		/// <summary>
		/// Return the damage resistance for a given damage type.
		/// If no resistance is found, the default resistance is 0.
		/// </summary>
		public float GetDamageResistance(DamageType damageType)
		{
			int damageTypeIndex = _damageResistances.FindIndex(x => x.damageType == damageType);
			return damageTypeIndex == -1 ? 0 : _damageSuceptibilities[damageTypeIndex].quantity;
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
			var args = new DamageEventArgs(damageQuantity);
			OnDamageReceivedEvent?.Invoke(this, args);
		}

        /// <summary>
        /// Take another bodylayer and copy its values to this one. Useful when spawning a new bodypart to preserve data.
        /// </summary>
        /// <param name="layer"> The layer from which we want the values to copy.</param>
		public void CopyLayerValues(BodyLayer other)
		{
			_damageResistances = other._damageResistances.Select(x => new DamageTypeQuantity(x.damageType, x.quantity)).ToList();
			_damageSuceptibilities = other._damageSuceptibilities.Select(x => new DamageTypeQuantity(x.damageType, x.quantity)).ToList();
			_damageTypeQuantities = other._damageTypeQuantities.Select(x => new DamageTypeQuantity(x.damageType, x.quantity)).ToList();
		}

		/// <summary>
		/// Set all resistances on this body layer. By default, there are none and resistance is 0.
		/// </summary>
		protected virtual void SetResistances()
		{
			return;
		}

		/// <summary>
		/// Set all susceptibilities on this body layer. By default, susceptibility is 1.
		/// </summary>
		protected virtual void SetSuceptibilities()
		{
			return;
		}

	}
}
