using System.Collections.Generic;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Coarse device state lines, readable at any range (examine.md §2, §7: powered, bolted, panel open).
    /// Only power exists so far; bolts and maintenance panels add keys here when they are built.
    /// </summary>
    public static class ExamineDeviceKeys
    {
        public const string Powered = "examine.device.powered";
        public const string PoweredFallback = "<k=good>Powered</k>.";
        public const string Unpowered = "examine.device.unpowered";
        public const string UnpoweredFallback = "<k=warn>Unpowered</k>.";

        public static readonly (string Key, string English)[] All =
        {
            (Powered, PoweredFallback), (Unpowered, UnpoweredFallback),
        };

        /// <summary>The power line for a device with a power consumer.</summary>
        public static void AppendPower(bool powered, List<ExamineSection> sections)
        {
            sections?.Add(new ExamineSection(powered
                ? ExamineText.Get(Powered, PoweredFallback)
                : ExamineText.Get(Unpowered, UnpoweredFallback)));
        }
    }
}
