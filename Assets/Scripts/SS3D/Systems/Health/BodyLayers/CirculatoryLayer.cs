using FishNet.Object;
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
            Damages[DamageType.Crush] = new (DamageType.Crush);
            Damages[DamageType.Slash] = new (DamageType.Slash);
            Damages[DamageType.Puncture] = new (DamageType.Puncture, 0f, 2f);
            Damages[DamageType.Pressure] = new (DamageType.Pressure);
            Damages[DamageType.Heat] = new (DamageType.Heat);
            Damages[DamageType.Cold] = new (DamageType.Cold);
            Damages[DamageType.Shock] = new (DamageType.Shock);
            Damages[DamageType.Rad] = new (DamageType.Rad);
            Damages[DamageType.Acid] = new (DamageType.Acid);
            Damages[DamageType.Toxic] = new (DamageType.Toxic, 0f, 1.5f);
            Damages[DamageType.Oxy] = new (DamageType.Oxy);
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
            SubstancesSubSystem registry = SubSystems.Get<SubstancesSubSystem>();
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
            OxygenConsumerSubSystem registry = SubSystems.Get<OxygenConsumerSubSystem>();
            registry.RegisterConsumer(this);
        }

        /// <summary>
        /// Should be called only when this circulatory layer does not function anymore (when body part is destroyed).
        /// </summary>
        [Server]
        public override void Cleanlayer()
        {
            OxygenConsumerSubSystem registry = SubSystems.Get<OxygenConsumerSubSystem>();
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
