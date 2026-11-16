namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Shared ID access handling for atmospheric machine interfaces.
    /// </summary>
    public abstract class AtmosMachineInterfaceBehaviour : AccessGatedMachineInterfaceBehaviour
    {
        protected override byte ReadIdControlId => MachineInterfaceControlIds.Atmos.ReadId;
    }
}
