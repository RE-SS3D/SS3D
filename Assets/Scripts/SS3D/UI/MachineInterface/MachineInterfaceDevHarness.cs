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
        private ApcPowerState _initialState = ApcPowerState.Nominal;

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
                _subsystem.SimulateState(_initialState);
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
                _subsystem.SimulateState(ApcPowerState.Nominal);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _subsystem.SimulateState(ApcPowerState.Overload);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _subsystem.SimulateState(ApcPowerState.Critical);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                _subsystem.Close();
            }
        }
    }
}
