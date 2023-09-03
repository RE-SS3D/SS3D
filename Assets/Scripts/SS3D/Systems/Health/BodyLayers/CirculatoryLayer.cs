using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Substances;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class CirculatoryLayer : BodyLayer, IOxygenConsumer, IOxygenNeeder
	{
        /// <summary>
        /// Mole quantity this layer can contain of oxygen
        /// </summary>
        private const double _oxygenMaxCapacity = 0.001f ;

        private double _oxygenReserve;

        private const float _damageWithNoOxygen = 5f;

        private const double _molesPerCubeCentimetersOfOxygenNeeded = 1.15e-9;

        /// <summary>
        /// max amount in mole of blood lost at each heart beat
        /// </summary>
        private const float MaxBloodLost = 2f;

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
            _oxygenReserve = _oxygenMaxCapacity;
            RegisterToOxygenConsumerSystem();
        }

		public CirculatoryLayer(BodyPart bodyPart,
		List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
		: base(bodyPart, damages, susceptibilities, resistances)
		{
            _oxygenReserve = _oxygenMaxCapacity;
            RegisterToOxygenConsumerSystem();
        }

		protected override void SetSuceptibilities()
		{
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
		}

        public void ConsumeOxygen()
        {
            double oxygenNeeded = OxygenNeeded;
            float proportionReserveOfNeeded =(float)(_oxygenReserve / oxygenNeeded);

            if (oxygenNeeded > _oxygenReserve)
            {
                _oxygenReserve = 0;
                InflictOxyDamage(proportionReserveOfNeeded);
            }
            else
            {
                _oxygenReserve -= oxygenNeeded;
            }
        }

        private void InflictOxyDamage(float proportionReserveOfNeeded)
        {
            var consumers = BodyPart.BodyLayers.OfType<IOxygenNeeder>();
            Debug.Log(consumers.Count());
            foreach (BodyLayer layer in consumers)
            {
                BodyPart.TryInflictDamage(layer.LayerType, new DamageTypeQuantity(DamageType.Oxy, (1-proportionReserveOfNeeded) * _damageWithNoOxygen));
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
            return _molesPerCubeCentimetersOfOxygenNeeded * BodyPart.Volume*1000;
        }

        /// <summary>
        /// Remove from the substance container a given amount of blood. For now, this amount is only determined by
        /// the damage 
        /// </summary>
        public void Bleed()
        {
            SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
            Substance blood = registry.FromType(SubstanceType.Blood);
            BodyPart.HealthController.Circulatory.Container.RemoveSubstance(blood, MaxBloodLost * RelativeDamage);
        }

        public void RegisterToOxygenConsumerSystem()
        {
            OxygenConsumerSystem registry = Subsystems.Get<OxygenConsumerSystem>();
            registry.RegisterConsumer(this);
        }

        public override void Cleanlayer()
        {
            OxygenConsumerSystem registry = Subsystems.Get<OxygenConsumerSystem>();
            registry.UnregisterConsumer(this);
        }
    }
}
