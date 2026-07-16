using System;

namespace SS3D.UI.MachineInterface
{
    public interface IMachineInterfaceBinder
    {
        event Action CloseRequested;

        event Action<byte, bool> BoolControlChanged;

        event Action<byte, float> NumericControlChanged;

        event Action<byte, int> ActionControlChanged;

        void Bind(IMachineInterfaceViewModel viewModel);

        void Disconnect();
    }
}
