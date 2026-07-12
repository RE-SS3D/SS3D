namespace SS3D.UI.MachineInterface
{
    public interface IMachineInterfaceClientBridge
    {
        void SetControl(byte controlId, bool value);

        void SetNumericControl(byte controlId, float delta);

        void SetActionControl(byte controlId, int value);

        void RequestClose();
    }
}
