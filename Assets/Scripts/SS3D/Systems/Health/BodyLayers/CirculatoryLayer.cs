using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Substances;
using System.Collections.Generic;
using System.Linq;


namespace SS3D.Systems.Health
{
    public class CirculatoryLayer : BodyLayer, IOxygenNeeder
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
        /// One continuous, dt-scaled metabolic step for this body part, driven by the CirculatoryController:
        /// take up to <paramref name="supply"/> mmol of oxygen into the reserve (never past capacity), then burn
        /// this tick's demand. If the reserve runs dry, inflict graded, dt-scaled Oxy damage in proportion to the
        /// unmet fraction. Returns the oxygen actually accepted, so the controller debits the pool exactly once.
        /// </summary>
        /// <param name="supply">Oxygen offered to this part this tick, in mmol.</param>
        /// <param name="dt">Elapsed time this tick, in seconds.</param>
        /// <returns>Oxygen accepted into the reserve, in mmol.</returns>
        [Server]
        public double MetabolicStep(double supply, float dt)
        {
            double space = _oxygenMaxCapacity - _oxygenReserve;
            double accepted = supply < space ? supply : space;
            if (accepted < 0d)
            {
                accepted = 0d;
            }

            _oxygenReserve += accepted;

            double demand = _oxygenNeeded * dt;
            _oxygenReserve -= demand;

            if (_oxygenReserve < 0d)
            {
                double deficit = -_oxygenReserve;
                _oxygenReserve = 0d;

                float deficitFraction = demand > 0d ? (float)(deficit / demand) : 0f;
                if (deficitFraction > 1f)
                {
                    deficitFraction = 1f;
                }

                float damage = deficitFraction * HealthConstants.DamageWithNoOxygen * dt;
                InflictOxyDamage(damage);
            }

            return accepted;
        }



        /// <summary>
        /// Inflict a given amount of Oxy damage on every oxygen-needing layer of this body part.
        /// </summary>
        /// <param name="amount">Oxy damage to apply to each needing layer.</param>
        [Server]
        private void InflictOxyDamage(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            foreach (BodyLayer layer in BodyPart.BodyLayers)
            {
                if (layer is IOxygenNeeder)
                {
                    BodyPart.TryInflictDamage(layer.LayerType, new(DamageType.Oxy, amount));
                }
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
        /// The CirculatoryController now owns registration with the metabolic scheduler and prunes this part via
        /// HealthController.OnBodyPartRemoved, so there is nothing to clean up here.
        /// </summary>
        [Server]
        public override void Cleanlayer()
        {
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
