﻿using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Substances;
using System.Collections.Generic;
using System.Linq;


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
		DamagesContainer damages, float oxygenReserveFactor)
		: base(bodyPart, damages)
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
                Log.Error(this, "Trying to set up a circulatory layer without a BleedingBodyPart component next to the body part." +
                    " Please add a BleedingBodyPart component.");
            }
        }

        protected override void SetDamagesContainer()
        {
            Damages.DamagesInfo.Add(DamageType.Crush, new BodyDamageInfo(DamageType.Crush, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Slash, new BodyDamageInfo(DamageType.Slash, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Puncture, new BodyDamageInfo(DamageType.Puncture, 0f, 2f, 0f));
            Damages.DamagesInfo.Add(DamageType.Pressure, new BodyDamageInfo(DamageType.Pressure, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Heat, new BodyDamageInfo(DamageType.Heat, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Cold, new BodyDamageInfo(DamageType.Cold, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Shock, new BodyDamageInfo(DamageType.Shock, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Rad, new BodyDamageInfo(DamageType.Rad, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Acid, new BodyDamageInfo(DamageType.Acid, 0f, 1f, 0f));
            Damages.DamagesInfo.Add(DamageType.Toxic, new BodyDamageInfo(DamageType.Toxic, 0f, 1.5f, 0f));
            Damages.DamagesInfo.Add(DamageType.Oxy, new BodyDamageInfo(DamageType.Oxy, 0f, 1f, 0f));
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
                    new(DamageType.Oxy, (1- fractionOfNeededOxygen) * HealthConstants.DamageWithNoOxygen));
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
            IEnumerable<IOxygenNeeder> oxygenNeeders = BodyPart.BodyLayers.OfType<IOxygenNeeder>();
            double totalOxygen = oxygenNeeders.Sum(x => x.GetOxygenNeeded());
            int numberOfConsumers = oxygenNeeders.Count();
            _oxygenNeeded = numberOfConsumers > 0 ? totalOxygen / numberOfConsumers : 0;
        }
    }
}
