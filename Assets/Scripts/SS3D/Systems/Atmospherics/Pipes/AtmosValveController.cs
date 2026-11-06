using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Connects two networks while open and parts them while closed (design §6). Open just
    /// equalises across the device, so flipping one costs no rebuild. A manual valve is turned by
    /// hand; a digital one is switched from its panel and needs power.
    /// </summary>
    public sealed class AtmosValveController : AtmosInlineDeviceBase
    {
        private const float ClosedHandleYaw = 90f;

        [SerializeField]
        private bool _startsOpen;

        [SerializeField]
        private bool _turnedByHand = true;

        // In line with the pipe while open, across it while closed.
        [SerializeField]
        private Transform _handle;

        [SerializeField]
        private Sprite _openIcon;

        [SerializeField]
        private Sprite _closedIcon;

        public override PumpRegulation Regulation => PumpRegulation.None;

        public bool IsOpen => IsEnabled;

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerSetEnabled(_startsOpen);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            if (!_turnedByHand)
                return base.CreateTargetInteractions(interactionEvent);

            return new IInteraction[]
            {
                new ToggleInteraction
                {
                    OnName = "Close valve",
                    OffName = "Open valve",
                    IconOn = _openIcon,
                    IconOff = _closedIcon,
                },
            };
        }

        /// <summary>
        /// Moves one tick's gas across an open valve, down the pressure gradient.
        /// </summary>
        public static float Equalise(GasPipeNetworkRegistry registry, GasPipeNetworkRecord first, GasPipeNetworkRecord second)
        {
            bool firstIsHigher = first.GetPressure(AtmosConstants.DefaultGasCount)
                >= second.GetPressure(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord high = firstIsHigher ? first : second;
            GasPipeNetworkRecord low = firstIsHigher ? second : first;

            float moles = AtmosPortFlow.ComputeEqualiseMoles(
                high.GetPressure(AtmosConstants.DefaultGasCount),
                high.Temperature,
                high.Volume,
                low.GetPressure(AtmosConstants.DefaultGasCount),
                low.Temperature,
                low.Volume);

            return registry.MoveGas(high, low, moles);
        }

        protected override void ApplyEnabledVisual(bool enabled)
        {
            if (_handle != null)
                _handle.localRotation = Quaternion.Euler(0f, enabled ? 0f : ClosedHandleYaw, 0f);
        }

        protected override bool RunInlineTick(
            GasPipeNetworkRegistry registry,
            GasPipeNetworkRecord inlet,
            GasPipeNetworkRecord outlet,
            float deltaTime)
        {
            float inletPressure = inlet.GetPressure(AtmosConstants.DefaultGasCount);
            float outletPressure = outlet.GetPressure(AtmosConstants.DefaultGasCount);
            return RecordFlow(Equalise(registry, inlet, outlet), inletPressure, outletPressure, deltaTime);
        }
    }
}
