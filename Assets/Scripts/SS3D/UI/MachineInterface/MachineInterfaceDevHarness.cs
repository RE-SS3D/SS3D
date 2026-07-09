using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Play-mode dev harness for previewing machine interfaces without networking.
    /// Attach alongside MachineInterfaceHost and MachineInterfaceSubSystem on a GameObject with UIDocument.
    /// </summary>
    public class MachineInterfaceDevHarness : Actor
    {
        [SerializeField]
        private bool _openOnStart = true;

        [SerializeField]
        private string _interfaceId = MachineInterfaceIds.Apc;

        [SerializeField]
        private ApcPowerState _initialApcState = ApcPowerState.Nominal;

        [SerializeField]
        private SmesPowerState _initialSmesState = SmesPowerState.Nominal;

        private MachineInterfaceSubSystem _subsystem;

        protected override void OnStart()
        {
            base.OnStart();

            if (!TryGetComponent(out _subsystem))
            {
                SubSystems.TryGet(out _subsystem);
            }

            if (_openOnStart && _subsystem != null)
            {
                SimulateCurrentInterface(_initialApcState, _initialSmesState);
            }
        }

        protected void Update()
        {
            if (_subsystem == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SimulateCurrentInterface(ApcPowerState.Nominal, SmesPowerState.Nominal);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SimulateCurrentInterface(ApcPowerState.Overload, SmesPowerState.Degraded);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SimulateCurrentInterface(ApcPowerState.Critical, SmesPowerState.Overload);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SimulateCurrentInterface(ApcPowerState.Critical, SmesPowerState.Fault);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                _subsystem.Close();
            }
        }

        private void SimulateCurrentInterface(ApcPowerState apcState, SmesPowerState smesState)
        {
            if (_interfaceId == MachineInterfaceIds.Smes)
            {
                _subsystem.SimulateSmesState(smesState);
            }
            else
            {
                _subsystem.SimulateApcState(apcState);
            }
        }
    }
}
