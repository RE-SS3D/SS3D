using SS3D.Systems.Electricity;

namespace SS3D.Systems.Atmospherics.Pipes
{
    internal static class AtmosPortPower
    {
        public static bool IsPowered(BasicPowerConsumer consumer) =>
            PowerGate.IsPowered(consumer, NullConsumerPolicy.Allow);
    }
}
