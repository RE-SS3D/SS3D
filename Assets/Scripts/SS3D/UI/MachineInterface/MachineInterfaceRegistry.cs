using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Runtime catalog of machine interface UI assets and binder factories.
    /// <see cref="MachineInterfaceHost"/> registers entries on awake; new machines add a
    /// <see cref="MachineInterfaceUiRegistration"/> there and a matching network handler in
    /// <see cref="MachineInterfaceNetworkRegistry"/>.
    /// </summary>
    public static class MachineInterfaceRegistry
    {
        private static readonly Dictionary<string, MachineInterfaceUiRegistration> UiRegistrations = new();

        public static void RegisterUi(MachineInterfaceUiRegistration registration)
        {
            UiRegistrations[registration.InterfaceId] = registration;
        }

        public static bool TryGetUi(string interfaceId, out MachineInterfaceUiRegistration registration)
        {
            return UiRegistrations.TryGetValue(interfaceId, out registration);
        }
    }
}
