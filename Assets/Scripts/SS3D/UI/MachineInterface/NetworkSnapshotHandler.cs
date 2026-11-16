using System;

namespace SS3D.UI.MachineInterface
{
    internal sealed class NetworkSnapshotHandler : INetworkSnapshotHandler
    {
        private readonly Func<object, string> _getInterfaceId;
        private readonly Func<object, IMachineInterfaceViewModel> _toViewModel;

        public NetworkSnapshotHandler(
            Func<object, string> getInterfaceId,
            Func<object, IMachineInterfaceViewModel> toViewModel)
        {
            _getInterfaceId = getInterfaceId;
            _toViewModel = toViewModel;
        }

        public string GetInterfaceId(object snapshot) => _getInterfaceId(snapshot);

        public IMachineInterfaceViewModel ToViewModel(object snapshot) => _toViewModel(snapshot);
    }
}
