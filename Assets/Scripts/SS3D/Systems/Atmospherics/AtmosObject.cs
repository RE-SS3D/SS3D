using Unity.Mathematics;

namespace SS3D.Systems.Atmospherics
{
 public struct AtmosObject
 {
        public AtmosState State;
        public int2 ChunkKey;

        public float4 VelocityNorth;
        public float4 VelocitySouth;
        public float4 VelocityEast;
        public float4 VelocityWest;

        public bool TemperatureSetting;

        public bool4 ActiveDirection;

        public AtmosObject(int2 chunkKey, float volume)
        {
            ChunkKey = chunkKey;
            State = AtmosState.Inactive;
            Volume = volume;    // One tile size
            Temperature = 293f; // Room temperature in Kelvin
            CoreGasses = 0f;
            TemperatureSetting = false;
            ActiveDirection = default;
            VelocityNorth = default;
            VelocitySouth = default;
            VelocityEast = default;
            VelocityWest = default;
        }

        public float Volume { get; private set; }

        public float Temperature { get; private set; }

        public float4 CoreGasses { get; private set; }

        public float Pressure => GasConstants.UseRealisticGasLaw ? GetRealPressure() : GetSimplifiedPressure();

        public bool IsEmpty => math.all(CoreGasses == 0f);

        public float4 CoreGassesProportions => TotalMoles != 0 ? CoreGasses / math.csum(CoreGasses) : 0;

        public float TotalMoles => math.csum(CoreGasses);

        /// <summary>
        /// The compressibility factor: How much does a gas deviate from the ideal gas law.
        /// </summary>
        public float CompressionFactor => GetRealVolume() / Volume;

        /// <summary>
        /// Returns the mass for the container in grams
        /// </summary>
        public float Mass => math.csum(CoreGasses * GasConstants.CoreGasDensity);

        public void SetBlocked() => State = AtmosState.Blocked;

        public void SetVacuum() => State = AtmosState.Vacuum;

        public void SetInactive() => State = AtmosState.Inactive;

        public void ClearCoreGasses()
        {
            CoreGasses = 0f;

            if (State == AtmosState.Active || State == AtmosState.Semiactive)
            {
                State = AtmosState.Inactive;
            }
        }

        public void MakeAir()
        {
            MakeEmpty();
            AddCoreGas(CoreAtmosGasses.Oxygen, 20.79f);
            AddCoreGas(CoreAtmosGasses.Nitrogen, 83.17f);
            Temperature = 293f;
        }

        public void AddCoreGasses(float4 amount)
        {
            CoreGasses += math.max(0, amount);
        }

        public void RemoveCoreGasses(float4 amount)
        {
            amount = math.max(0, amount);
            CoreGasses = math.max(0, CoreGasses - amount);
        }

        public void AddCoreGas(CoreAtmosGasses gas, float amount)
        {
            amount = math.max(0, amount);
            float4 coreGasses = CoreGasses;
            coreGasses[(int)gas] = math.max(coreGasses[(int)gas] + amount, 0f);
            CoreGasses = coreGasses;
        }

        public void RemoveCoreGas(CoreAtmosGasses gas, float amount)
        {
            amount = math.max(0, amount);
            float4 coreGasses = CoreGasses;
            coreGasses[(int)gas] = math.max(coreGasses[(int)gas] - amount, 0f);
            CoreGasses = coreGasses;
        }

        public void AddHeat(float amount)
        {
            // todo check formula from amount in joules
            amount = math.max(0, amount);
            Temperature += amount;
        }

        public void RemoveHeat(float amount)
        {
            // todo check formula from amount in joules
            amount = math.max(0, amount);
            Temperature = math.max(0, Temperature - amount);
        }

        public bool IsAir()
        {
            float oxygen = CoreGasses[(int)CoreAtmosGasses.Oxygen];
            float nitrogen = CoreGasses[(int)CoreAtmosGasses.Nitrogen];

            bool oxyDiff = math.abs(oxygen - 20.79f) < 0.1f;
            bool nitroDiff = math.abs(nitrogen - 83.17f) < 0.1f;

            return oxyDiff && nitroDiff;
        }

        public void MakeRandom()
        {
            MakeEmpty();

            for (int i = 0; i < 4; i++)
            {
                AddCoreGas((CoreAtmosGasses)i, UnityEngine.Random.Range(0, 300f));
            }
        }

        public void SetTemperature(float temperature)
        {
            Temperature = math.max(temperature, 0f);
        }

        public override string ToString()
        {
            string text = $"State: {State}, Pressure: {Pressure}, Gasses: {CoreGasses}\n";
            text += $"Temperature: {Temperature} Kelvin, Compressibility factor: {CompressionFactor}\n";
            return text;
        }

        public float GetSpecificHeat()
        {
            return math.csum(CoreGasses * GasConstants.CoreSpecificHeat) / TotalMoles;
        }

        public float GetVolume()
        {
            return GasConstants.UseRealisticGasLaw ? GetRealVolume() : GetSimplifiedVolume();
        }

        public float4 GetAllPartialPressures()
        {
            return GasConstants.UseRealisticGasLaw ? GetAllSimplifiedPartialPressures() : GetAllRealPartialPressures();
        }

        public float GetPartialPressure(CoreAtmosGasses gas)
        {
            float pressure = (CoreGasses[(int)gas] * GasConstants.GasConstant * Temperature) / Volume / 1000f;

            return float.IsNaN(pressure) ? 0f : pressure;
        }

        public void MakeEmpty()
        {
            CoreGasses = 0f;
        }

        private float4 GetAllSimplifiedPartialPressures() => CoreGasses * GasConstants.GasConstant * Temperature / Volume / 1000f;

        private float GetSimplifiedVolume()
        {
            return Volume;
        }

        /// <summary>
        /// Returns the realistic volume in the container by substracting the molecular volume from
        /// the ideal volume.
        /// In simpler terms, at big qantities of gas (moles), the size of the molecules start playing
        /// a role and reduce the effective size of a container. Thus increasing pressure.
        /// </summary>
        /// <returns></returns>
        private float GetRealVolume()
        {
            return Volume - math.csum(CoreGasses * GasConstants.CoreGasDensity * math.pow(10, -6));
        }

        /// <summary>
        /// Returns the pressure based on the ideal gas law
        /// </summary>
        /// <returns></returns>
        private float GetSimplifiedPressure()
        {
            float pressure = TotalMoles * GasConstants.GasConstant * Temperature / Volume / 1000f;

            if (math.isnan(pressure))
            {
                return 0f;
            }

            return pressure;
        }

        /// <summary>
        /// Returns the real pressure based on Van der Waals equation of state. Essentially this is the
        /// ideal gas law but it takes intermolecular interactions into account and accounts for the real
        /// volume that gas molecules take up.
        /// P = Pressure in kPa
        /// a = Constant that differs per gas
        /// n = Number of moles
        /// V = volume
        /// R = Universal gas constant
        /// b = Volume that is occupied by one mole of the molecules.
        /// P = a(n^2 / V^2) + nRT / (V - nb)
        /// </summary>
        /// <returns></returns>
        private float GetRealPressure()
        {
            float pressure = math.csum(GetAllRealPartialPressures());

            if (math.isnan(pressure))
            {
                return 0f;
            }

            return pressure;
        }

        private float4 GetAllRealPartialPressures()
        {
            return ((CoreGasses * GasConstants.GasConstant * Temperature / GetRealVolume()) / 1000f) +
                (100 * (GasConstants.InterMolecularInteraction * math.pow(CoreGasses, 2)) / math.pow(Volume * 1000, 2));
        }
    }
}
