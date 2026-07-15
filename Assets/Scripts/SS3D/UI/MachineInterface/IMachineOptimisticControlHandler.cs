using System;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Optional side-effects for optimistic control application (e.g. APC channel diagnostics).
    /// </summary>
    public readonly struct MachineOptimisticControlCallbacks
    {
        public MachineOptimisticControlCallbacks(Action<string, bool> channelToggled)
        {
            ChannelToggled = channelToggled;
        }

        public Action<string, bool> ChannelToggled { get; }
    }

    /// <summary>
    /// Client-side optimistic mutations for an open machine view-model type.
    /// </summary>
    public interface IMachineOptimisticControlHandler
    {
        Type ViewModelType { get; }

        void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks);

        void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks);

        void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks);
    }

    /// <summary>
    /// Registry of optimistic control handlers keyed by view-model type.
    /// </summary>
    public static class MachineOptimisticControlRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<Type, IMachineOptimisticControlHandler> Handlers = new();

        private static bool _registered;

        public static void EnsureRegistered()
        {
            if (_registered)
            {
                return;
            }

            Register(new ApcOptimisticControlHandler());
            Register(new SmesOptimisticControlHandler());
            Register(new VendingOptimisticControlHandler());
            Register(new ScrubberOptimisticControlHandler());
            Register(new VentOptimisticControlHandler());
            Register(new PumpOptimisticControlHandler());
            Register(new AirAlarmOptimisticControlHandler());
            _registered = true;
        }

        public static void Register(IMachineOptimisticControlHandler handler)
        {
            Handlers[handler.ViewModelType] = handler;
        }

        public static bool TryGet(Type viewModelType, out IMachineOptimisticControlHandler handler) =>
            Handlers.TryGetValue(viewModelType, out handler);
    }
}
