using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Server-side ID access session handling for machine interfaces that gate controls behind a credential check.
    /// </summary>
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public abstract class AccessGatedMachineInterfaceBehaviour : MachineInterfaceBehaviour
    {
        private const float IdScanDelaySeconds = 0.8f;

        [SyncVar(OnChange = nameof(OnAccessStateChanged))]
        private bool _accessGranted;

        [SyncVar(OnChange = nameof(OnAccessStateChanged))]
        private bool _accessScanning;

        [SyncVar(OnChange = nameof(OnAccessStateChanged))]
        private bool _accessDenied;

        private Coroutine _scanCoroutine;
        private NetworkConnection _scanConnection;
        private AuthLogDeviceBehaviour _authLogDevice;

        protected bool AccessGranted => _accessGranted;

        protected bool AccessScanning => _accessScanning;

        protected bool AccessDenied => _accessDenied;

        protected virtual AccessMask RequiredAccess =>
            AccessMask.FromLevels(AccessLevel.Engineering);

        protected abstract byte ReadIdControlId { get; }

        protected override void OnAwake()
        {
            base.OnAwake();
            _authLogDevice = GetComponent<AuthLogDeviceBehaviour>();
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId != ReadIdControlId)
            {
                return false;
            }

            if (!IsServer)
            {
                return true;
            }

            HandleReadIdAction(LastControlConnection);
            return true;
        }

        [Server]
        private void HandleReadIdAction(NetworkConnection conn)
        {
            if (_accessScanning)
            {
                return;
            }

            if (_accessGranted)
            {
                _accessGranted = false;
                _accessDenied = false;
                RefreshAllViewers();
                return;
            }

            _accessScanning = true;
            _accessDenied = false;
            _scanConnection = conn;
            RefreshAllViewers();

            if (_scanCoroutine != null)
            {
                StopCoroutine(_scanCoroutine);
            }

            _scanCoroutine = StartCoroutine(CompleteIdScan());
        }

        private IEnumerator CompleteIdScan()
        {
            yield return new WaitForSeconds(IdScanDelaySeconds);

            if (!IsServer)
            {
                yield break;
            }

            _scanCoroutine = null;
            _accessScanning = false;

            if (MachineInterfaceOperatorResolver.TryResolveInventory(_scanConnection, out HumanInventory inventory)
                && SubSystems.TryGet(out IdAccessSubSystem idAccess))
            {
                _accessGranted = idAccess.CheckAccess(inventory, RequiredAccess, _authLogDevice).Passed;
            }
            else
            {
                _accessGranted = false;
            }

            _accessDenied = !_accessGranted;
            _scanConnection = null;
            RefreshAllViewers();
        }

        private void OnAccessStateChanged(bool _, bool __, bool asServer)
        {
            if (asServer)
            {
                RefreshAllViewers();
            }
        }

        protected override void OnDestroyed()
        {
            if (_scanCoroutine != null)
            {
                StopCoroutine(_scanCoroutine);
                _scanCoroutine = null;
            }

            base.OnDestroyed();
        }
    }
}
