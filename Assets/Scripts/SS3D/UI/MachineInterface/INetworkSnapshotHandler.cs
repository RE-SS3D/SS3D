namespace SS3D.UI.MachineInterface
{
    internal interface INetworkSnapshotHandler
    {
        string GetInterfaceId(object snapshot);

        IMachineInterfaceViewModel ToViewModel(object snapshot);
    }
}
