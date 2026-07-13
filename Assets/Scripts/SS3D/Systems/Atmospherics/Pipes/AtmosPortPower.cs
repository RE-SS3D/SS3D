using System.Electricity;

namespace SS3D.Systems.Atmospherics.Pipes
{
    internal static class AtmosPortPower
    {
        public static bool IsPowered(BasicPowerConsumer consumer) =>
            consumer == null || consumer.PowerStatus == PowerStatus.Powered;
    }
}
