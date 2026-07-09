using System;
using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public static class MachineInterfaceNetworkRegistry
    {
        private static readonly Dictionary<Type, INetworkSnapshotHandler> Handlers = new();

        static MachineInterfaceNetworkRegistry()
        {
            Register<ApcInterfaceSnapshot>(
                GetApcInterfaceId,
                ApcInterfaceSnapshotMapper.ToViewModel);
            Register<SmesInterfaceSnapshot>(
                GetSmesInterfaceId,
                SmesInterfaceSnapshotMapper.ToViewModel);
        }

        public static void DispatchOpen<TSnapshot>(
            MachineInterfaceSubSystem subsystem,
            TSnapshot snapshot,
            IMachineInterfaceClientBridge bridge)
            where TSnapshot : struct
        {
            if (!Handlers.TryGetValue(typeof(TSnapshot), out INetworkSnapshotHandler handler))
            {
                return;
            }

            subsystem.OpenFromNetwork(
                handler.GetInterfaceId(snapshot),
                handler.ToViewModel(snapshot),
                bridge);
        }

        public static void DispatchRefresh<TSnapshot>(MachineInterfaceSubSystem subsystem, TSnapshot snapshot)
            where TSnapshot : struct
        {
            if (!Handlers.TryGetValue(typeof(TSnapshot), out INetworkSnapshotHandler handler))
            {
                return;
            }

            subsystem.RefreshFromNetwork(handler.GetInterfaceId(snapshot), handler.ToViewModel(snapshot));
        }

        private static string GetApcInterfaceId(ApcInterfaceSnapshot snapshot) => snapshot.InterfaceId;

        private static string GetSmesInterfaceId(SmesInterfaceSnapshot snapshot) => snapshot.InterfaceId;

        private static void Register<TSnapshot>(
            Func<TSnapshot, string> getInterfaceId,
            Func<TSnapshot, IMachineInterfaceViewModel> toViewModel)
            where TSnapshot : struct
        {
            Handlers[typeof(TSnapshot)] = new NetworkSnapshotHandler(
                snapshot => getInterfaceId((TSnapshot)snapshot),
                snapshot => toViewModel((TSnapshot)snapshot));
        }
    }
}
