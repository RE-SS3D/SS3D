namespace SS3D.UI.MachineInterface
{
    public interface IAccessGatedInterfaceViewModel
    {
        bool AccessGranted { get; set; }

        bool AccessScanning { get; set; }

        bool AccessDenied { get; set; }
    }
}
