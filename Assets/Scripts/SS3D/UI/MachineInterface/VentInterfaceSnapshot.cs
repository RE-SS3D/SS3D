namespace SS3D.UI.MachineInterface
{
  public struct VentInterfaceSnapshot
  {
    public int MachineObjectId;

    public string InterfaceId;

    public string Title;

    public string ModelLabel;

    public string DeviceTitle;

    public string Subtitle;

    public bool PowerOk;

    public bool Powered;

    public bool Connected;

    public byte Scenario;

    public bool AccessGranted;

    public bool AccessScanning;

    public int TargetPressureKpa;
  }
}
