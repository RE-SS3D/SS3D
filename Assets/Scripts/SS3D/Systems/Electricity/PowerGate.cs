using SS3D.Core;
using SS3D.Systems.Area;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Null-consumer policy when no <see cref="IPowerConsumer"/> is attached.
    /// </summary>
    public enum NullConsumerPolicy
    {
        /// <summary>Treat missing consumer as powered (legacy atmos/furniture paths).</summary>
        Allow,

        /// <summary>Treat missing consumer as unpowered (switches, vendors, visuals).</summary>
        Deny,
    }

    /// <summary>
    /// Shared power and APC channel gating for furniture, atmos, lighting, and machine UI.
    /// </summary>
    public static class PowerGate
    {
        public static bool IsChannelEnabled(PowerChannel channel, ApcControlFlags enabledChannels)
        {
            ApcControlFlags flag = channel switch
            {
                PowerChannel.Lighting => ApcControlFlags.Lighting,
                PowerChannel.Environment => ApcControlFlags.Environment,
                _ => ApcControlFlags.Equipment,
            };

            return (enabledChannels & flag) != 0;
        }

        /// <summary>
        /// Returns true when the consumer's APC channel is enabled, or when no area APC applies (passthrough).
        /// </summary>
        public static bool IsChannelOpen(IPowerConsumer consumer)
        {
            if (consumer == null)
            {
                return true;
            }

            if (consumer is not IElectricDevice device
                || !SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                || !areaSubSystem.TryGetEffectiveApcForDevice(device, out IApcChannelSource apc))
            {
                return true;
            }

            return IsChannelEnabled(consumer.Channel, apc.Channels);
        }

        /// <summary>
        /// Powered check that trusts simulation <see cref="PowerStatus"/> (channels already applied on the tick path).
        /// </summary>
        public static bool IsPowered(IPowerConsumer consumer, NullConsumerPolicy nullPolicy)
        {
            if (consumer == null)
            {
                return nullPolicy == NullConsumerPolicy.Allow;
            }

            return consumer.PowerStatus == PowerStatus.Powered;
        }

        /// <summary>
        /// Powered check that also re-AND's APC channel state (for visuals that may run between ticks).
        /// </summary>
        public static bool IsEffectivelyPowered(IPowerConsumer consumer, NullConsumerPolicy nullPolicy)
        {
            if (consumer == null)
            {
                return nullPolicy == NullConsumerPolicy.Allow;
            }

            if (!IsChannelOpen(consumer))
            {
                return false;
            }

            return consumer.PowerStatus == PowerStatus.Powered;
        }
    }
}
