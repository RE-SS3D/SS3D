using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Scrubber UI filter keys, in the order the toggles send them.
    /// </summary>
    public static class ScrubberGasFilters
    {
        public const int KeyCount = 4;

        public static readonly string[] Keys = { "O2", "N2", "CO2", "Toxins" };

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
                ["O2"] = false,
                ["N2"] = false,
                ["CO2"] = true,
                ["Toxins"] = false,
            };
        }

        public static void CopyToDictionary(bool o2, bool n2, bool co2, bool toxins, Dictionary<string, bool> target)
        {
            target ??= new Dictionary<string, bool>();
            target["O2"] = o2;
            target["N2"] = n2;
            target["CO2"] = co2;
            target["Toxins"] = toxins;
        }
    }
}
