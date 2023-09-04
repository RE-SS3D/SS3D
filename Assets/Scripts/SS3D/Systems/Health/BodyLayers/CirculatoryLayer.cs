using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Substances;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace SS3D.Systems.Health
{
    public class CirculatoryLayer : BodyLayer, IOxygenConsumer, IOxygenNeeder
	{
        /// <summary>
        /// MilliMole quantity this layer can contain of oxygen
        /// </summary>
        private double _oxygenMaxCapacity;

        /// <summary>
        /// Millimole quantity of oxygen in reserve in this circulatory layer.
        /// </summary>
        private double _oxygenReserve;

        private GameObject _bloodEffect;

        /// <summary>
        /// To keep things simple for now, a body part simply needs the average of oxygen consumed for each consuming layer composing it.
        /// </summary>
        public double OxygenNeeded
        {
            get
            {
                double totalOxygen = BodyPart.BodyLayers.OfType<IOxygenNeeder>().Sum(x => x.GetOxygenNeeded());
                int NumberOxygenConsumers = BodyPart.BodyLayers.OfType<IOxygenNeeder>().Count();
                if (NumberOxygenConsumers > 0)
                {
                    return totalOxygen / NumberOxygenConsumers;
                }
                else return 0;
            }   
        }
        
		public override BodyLayerType LayerType
		{
			get { return BodyLayerType.Circulatory; }
		}
		public CirculatoryLayer(BodyPart bodyPart) : base(bodyPart)
		{
            // Should approximately correspond to three seconds of oxygen reserve at 60 bmp heart rate.
            _oxygenMaxCapacity = BodyPart.Volume * HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody *
                HealthConstants.OxygenSecondOfReserveInNormalConditions;
            _oxygenReserve = _oxygenMaxCapacity;
            RegisterToOxygenConsumerSystem();
        }

		public CirculatoryLayer(BodyPart bodyPart,
		List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
		: base(bodyPart, damages, susceptibilities, resistances)
		{
            _oxygenMaxCapacity = BodyPart.Volume * HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody *
                HealthConstants.OxygenSecondOfReserveInNormalConditions;
            _oxygenReserve = _oxygenMaxCapacity;
            RegisterToOxygenConsumerSystem();
        }

		protected override void SetSuceptibilities()
		{
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
		}

        protected override void DamageInflicted(DamageTypeQuantity damageQuantity)
        {
            base.DamageInflicted(damageQuantity);
            GameObject bleedingEffect = Assets.Get<GameObject>(AssetDatabases.ParticlesEffects, (int)ParticlesEffectsIds.BleedingParticle);
            if (_bloodEffect != null) return;

            GameObject bloodDisplayer;
            Transform bloodParent;
            if(BodyPart.BodyCollider != null)
            {
                bloodDisplayer = BodyPart.BodyCollider.gameObject;
                bloodParent= BodyPart.BodyCollider.gameObject.transform;
            }
            else
            {
                bloodDisplayer = BodyPart.gameObject;
                bloodParent = BodyPart.gameObject.transform;
            }

            _bloodEffect = Object.Instantiate(bleedingEffect, bloodDisplayer.transform.position, Quaternion.identity);
            _bloodEffect.transform.parent = bloodParent;
        }

        /// <summary>
        /// Consume oxygen and inflict damages if not enough oxygen is present.
        /// </summary>
        public void ConsumeOxygen()
        {
            double oxygenNeeded = OxygenNeeded;
            float fractionOfNeededOxygen =(float)(_oxygenReserve / oxygenNeeded);

            if (oxygenNeeded > _oxygenReserve)
            {
                _oxygenReserve = 0;
                InflictOxyDamage(fractionOfNeededOxygen);
            }
            else
            {
                _oxygenReserve -= oxygenNeeded;
            }
        }



        /// <summary>
        /// Inflict Oxy damage to all body layers needing oxygen, in proportion of what's left in reserve.
        /// </summary>
        /// <param name="fractionOfNeededOxygen"> oxygen in reserve divided by needed oxygen. Should be between 0 and 1.</param>
        private void InflictOxyDamage(float fractionOfNeededOxygen)
        {
            var consumers = BodyPart.BodyLayers.OfType<IOxygenNeeder>();
            Debug.Log(consumers.Count());
            foreach (BodyLayer layer in consumers)
            {
                BodyPart.TryInflictDamage(layer.LayerType,
                    new DamageTypeQuantity(DamageType.Oxy, (1- fractionOfNeededOxygen) * HealthConstants.DamageWithNoOxygen));
            }
        }

        public void ReceiveOxygen(double mole)
        {
            if(_oxygenReserve + mole > _oxygenMaxCapacity)
            {
                _oxygenReserve = _oxygenMaxCapacity;
            }
            else
            {
                _oxygenReserve += mole;
            }
        }

        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * BodyPart.Volume;
        }

        /// <summary>
        /// Remove from the substance container a given amount of blood. For now, this amount is only determined by
        /// the damage. TODO : different kind of damages should contribute differently to bleeding.
        /// </summary>
        public void Bleed()
        {
            SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
            Substance blood = registry.FromType(SubstanceType.Blood);
            BodyPart.HealthController.Circulatory.Container.RemoveSubstance(blood, HealthConstants.MaxBloodLost * RelativeDamage);
        }

        /// <summary>
        /// Called when this layer is created, necessary for periodic oxygen consumption.
        /// </summary>
        public void RegisterToOxygenConsumerSystem()
        {
            OxygenConsumerSystem registry = Subsystems.Get<OxygenConsumerSystem>();
            registry.RegisterConsumer(this);
        }

        /// <summary>
        /// Should be called only when this circulatory layer does not function anymore (when body part is destroyed).
        /// </summary>
        public override void Cleanlayer()
        {
            OxygenConsumerSystem registry = Subsystems.Get<OxygenConsumerSystem>();
            registry.UnregisterConsumer(this);
        }
    }
}
