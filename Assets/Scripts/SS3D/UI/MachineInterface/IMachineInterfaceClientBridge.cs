namespace SS3D.UI.MachineInterface
{
    public interface IMachineInterfaceClientBridge
    {
        void SetControl(byte controlId, bool value);

        void RequestClose();
    }
}
