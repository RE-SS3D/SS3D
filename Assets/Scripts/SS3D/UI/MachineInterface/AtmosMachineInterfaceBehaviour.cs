using FishNet.Connection;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Atmospherics.Pipes;
using System.Collections;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Shared ID access handling for atmospheric machine interfaces.
    /// </summary>
    public abstract class AtmosMachineInterfaceBehaviour : MachineInterfaceBehaviour
    {
        private const float IdScanDelaySeconds = 0.8f;

        [SyncVar(OnChange = nameof(OnAccessStateChanged))]
        private bool _accessGranted;

        [SyncVar(OnChange = nameof(OnAccessStateChanged))]
        private bool _accessScanning;

        private Coroutine _scanCoroutine;

        protected bool AccessGranted => _accessGranted;

        protected bool AccessScanning => _accessScanning;

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.ReadId)
            {
                return false;
            }

            if (!IsServer)
            {
                return true;
            }

            HandleReadIdAction();
            return true;
        }

        [Server]
        private void HandleReadIdAction()
        {
            if (_accessScanning)
            {
                return;
            }

            if (_accessGranted)
            {
                _accessGranted = false;
                RefreshAllViewers();
                return;
            }

            _accessScanning = true;
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
            _accessGranted = true;
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
