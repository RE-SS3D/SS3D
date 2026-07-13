using SS3D.Systems.Atmospherics;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Maps scrubber UI filter keys to atmos gas slots.
    /// </summary>
    public static class ScrubberGasFilters
    {
        public const int KeyCount = 5;

        public static readonly string[] Keys = { "O2", "N2", "CO2", "Plasma", "Toxins" };

        public static bool TryGetGasId(int filterIndex, out GasId gasId)
        {
            gasId = default;
            return filterIndex switch
            {
                0 => Assign(AtmosConstants.Oxygen, out gasId),
                1 => Assign(AtmosConstants.Nitrogen, out gasId),
                2 => Assign(AtmosConstants.CarbonDioxide, out gasId),
                3 => Assign(AtmosConstants.Plasma, out gasId),
                _ => false,
            };
        }

        public static int IndexOfKey(string key)
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] == key)
                {
                    return i;
                }
            }

            return -1;
        }

        public static Dictionary<string, bool> CreateDefaultMap()
        {
            return new Dictionary<string, bool>
            {
                ["O2"] = true,
                ["N2"] = true,
                ["CO2"] = true,
                ["Plasma"] = false,
                ["Toxins"] = true,
            };
        }

        public static void CopyToDictionary(bool o2, bool n2, bool co2, bool plasma, bool toxins, Dictionary<string, bool> target)
        {
            target ??= new Dictionary<string, bool>();
            target["O2"] = o2;
            target["N2"] = n2;
            target["CO2"] = co2;
            target["Plasma"] = plasma;
            target["Toxins"] = toxins;
        }

        private static bool Assign(GasId source, out GasId gasId)
        {
            gasId = source;
            return true;
        }
    }
}
