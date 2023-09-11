using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Logging;
using SS3D.Substances;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace SS3D.Systems.Health
{
    public class CirculatoryLayer : BodyLayer, IOxygenConsumer, IOxygenNeeder
	{
        /// <summary>
        /// MilliMole quantity this layer can contain of oxygen.
        /// </summary>
        private double _oxygenMaxCapacity;

        /// <summary>
        /// Millimole quantity of oxygen in reserve in this circulatory layer.
        /// </summary>
        private double _oxygenReserve;

        private BleedingBodyPart _bleedingHandler;

        private double _oxygenNeeded;

        /// <summary>
        /// To keep things simple for now, 
        /// a body part simply needs the average of 
        /// oxygen consumed for each consuming layer composing it.
        /// </summary>
        public double OxygenNeeded
        {
            private set => SetOxygenNeeded();
            get => _oxygenNeeded;
        }
        
		public override BodyLayerType LayerType
		{
			get { return BodyLayerType.Circulatory; }
		}

        /// <summary>
        /// </summary>
        /// <param name="bodyPart"></param>
        /// <param name="oxygenReserveFactor">The oxygen reserve factor defines how much oxygen this bodylayer can store.
        /// In normal conditions, it should roughly be equal to the time in seconds of reserve</param>
		public CirculatoryLayer(BodyPart bodyPart, float oxygenReserveFactor) : base(bodyPart)
		{
            // Should approximately correspond to "OxygenSecondOfReserveInNormalConditions * oxygenReserveFactor"
            // seconds of oxygen reserve at 60 bmp heart rate.
            Init(bodyPart, oxygenReserveFactor);
        }

		public CirculatoryLayer(BodyPart bodyPart,
		List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances, float oxygenReserveFactor)
		: base(bodyPart, damages, susceptibilities, resistances)
		{
            Init(bodyPart, oxygenReserveFactor);
        }

        private void Init(BodyPart bodyPart, float oxygenReserveFactor)
        {
            _oxygenMaxCapacity = BodyPart.Volume * HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * oxygenReserveFactor;
            _oxygenReserve = _oxygenMaxCapacity;

            // TODO : Currently only set the amount of oxygen needed once at Init.
            // Should maybe change too if a layer is changing the amount of oxygen it needs,
            // or if it gets destroyed or one gets added.
            SetOxygenNeeded();

            RegisterToOxygenConsumerSystem();
            if(bodyPart.TryGetComponent(out BleedingBodyPart bleedingBodyPart))
            {
                _bleedingHandler = bleedingBodyPart; 
            }
            else
            {
                Punpun.Error(this, "Trying to set up a circulatory layer without a BleedingBodyPart component next to the body part." +
                    " Please add a BleedingBodyPart component.");
            }
        }

        [Server]
		protected override void SetSuceptibilities()
		{
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 2f));
			_damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
		}

        /// <summary>
        /// Consume oxygen and inflict damages if not enough oxygen is present.
        /// </summary>
        [Server]
        public void ConsumeOxygen()
        {
            float fractionOfNeededOxygen =(float)(_oxygenReserve / _oxygenNeeded);

            if (_oxygenNeeded > _oxygenReserve)
            {
                _oxygenReserve = 0;
                InflictOxyDamage(fractionOfNeededOxygen);
            }
            else
            {
                _oxygenReserve -= _oxygenNeeded;
            }
        }



        /// <summary>
        /// Inflict Oxy damage to all body layers needing oxygen, in proportion of what's left in reserve.
        /// </summary>
        /// <param name="fractionOfNeededOxygen"> oxygen in reserve divided by needed oxygen. Should be between 0 and 1.</param>
        [Server]
        private void InflictOxyDamage(float fractionOfNeededOxygen)
        {
            var consumers = BodyPart.BodyLayers.OfType<IOxygenNeeder>();
            foreach (BodyLayer layer in consumers)
            {
                BodyPart.TryInflictDamage(layer.LayerType,
                    new DamageTypeQuantity(DamageType.Oxy, (1- fractionOfNeededOxygen) * HealthConstants.DamageWithNoOxygen));
            }
        }

        [Server]
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

        [Server]
        public double GetOxygenNeeded()
        {
            return HealthConstants.MilliMolesOfOxygenPerMillilitersOfBody * BodyPart.Volume;
        }

        /// <summary>
        /// Remove from the substance container a given amount of blood. For now, this amount is only determined by
        /// the amount of damage. TODO : different kind of damages should contribute differently to bleeding.
        /// </summary>
        [Server]
        public void Bleed()
        {
            SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
            Substance blood = registry.FromType(SubstanceType.Blood);
            BodyPart.HealthController.Circulatory.Container.RemoveSubstance(blood, HealthConstants.MaxBloodLost * RelativeDamage);

            if (!_bleedingHandler.isBleeding && RelativeDamage > 0)
            {
                _bleedingHandler.isBleeding = true;
            }
            else if ((_bleedingHandler.isBleeding && RelativeDamage == 0))
            {
                _bleedingHandler.isBleeding = false;
            }
        }

        /// <summary>
        /// Called when this layer is created, necessary for periodic oxygen consumption.
        /// </summary>
        [Server]
        public void RegisterToOxygenConsumerSystem()
        {
            OxygenConsumerSystem registry = Subsystems.Get<OxygenConsumerSystem>();
            registry.RegisterConsumer(this);
        }

        /// <summary>
        /// Should be called only when this circulatory layer does not function anymore (when body part is destroyed).
        /// </summary>
        [Server]
        public override void Cleanlayer()
        {
            OxygenConsumerSystem registry = Subsystems.Get<OxygenConsumerSystem>();
            registry.UnregisterConsumer(this);
        }

        [Server]
        private void SetOxygenNeeded()
        {
            double totalOxygen = BodyPart.BodyLayers.OfType<IOxygenNeeder>().Sum(x => x.GetOxygenNeeded());
            int NumberOxygenConsumers = BodyPart.BodyLayers.OfType<IOxygenNeeder>().Count();
            if (NumberOxygenConsumers > 0)
            {
                _oxygenNeeded = totalOxygen / NumberOxygenConsumers;
            }
            else _oxygenNeeded = 0;
        }
    }
}
