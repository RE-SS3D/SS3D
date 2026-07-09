using System;

namespace SS3D.UI.MachineInterface
{
    public interface IMachineInterfaceBinder
    {
        event Action CloseRequested;

        event Action<byte, bool> BoolControlChanged;

        event Action<byte, float> NumericControlChanged;

        void Bind(IMachineInterfaceViewModel viewModel);

        void Disconnect();
    }
}
