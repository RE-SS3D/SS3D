namespace SS3D.UI.MachineInterface
{
  public struct AirAlarmInterfaceSnapshot
  {
    public int MachineObjectId;

    public string InterfaceId;

    public string Title;

    public string ModelLabel;

    public string DeviceTitle;

    public string Subtitle;

    public bool PowerOk;

    public byte Scenario;

    public bool AccessGranted;

    public bool AccessScanning;

    public float PressureKpa;

    public float OxygenFraction;

    public float CarbonDioxideFraction;
  }
}
