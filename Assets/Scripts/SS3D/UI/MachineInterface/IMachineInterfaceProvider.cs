using FishNet.Object;

namespace SS3D.UI.MachineInterface
{
    public interface IMachineInterfaceProvider
    {
        string InterfaceId { get; }

        NetworkObject NetworkObject { get; }
    }
}
