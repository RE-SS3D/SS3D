using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Selection;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public sealed class IdConsoleController : MachineInterfaceBehaviour
    {
        private readonly List<string> _editLog = new();

        [SerializeField]
        private AttachedContainer _targetCardSlot;

        [SerializeField]
        private string _title = "ID CONSOLE";

        [SerializeField]
        private string _subtitle = "Personnel Access Management";

        [SerializeField]
        private string _modelLabel = "IDC-1 · crew credential editor";

        private AuthLogDeviceBehaviour _authLogDevice;

        public override string InterfaceId => MachineInterfaceIds.IdConsole;

        private static bool TryResolveOperatorInventory(NetworkConnection conn, out HumanInventory inventory) =>
            MachineInterfaceOperatorResolver.TryResolveInventory(conn, out inventory);

        private static void SetLogEntry(ref IdConsoleInterfaceSnapshot snapshot, int index, string value)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Log0 = value;
                    break;
                }

                case 1:
                {
                    snapshot.Log1 = value;
                    break;
                }

                case 2:
                {
                    snapshot.Log2 = value;
                    break;
                }

                case 3:
                {
                    snapshot.Log3 = value;
                    break;
                }
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _authLogDevice = GetComponent<AuthLogDeviceBehaviour>();
        }

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            TargetOpenInterface(conn, BuildSnapshot(conn));
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            TargetRefreshInterface(conn, BuildSnapshot(conn));
        }

        protected override bool ApplyControl(byte controlId, bool value)
        {
            return false;
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId != MachineInterfaceControlIds.IdConsole.ToggleAccessLevel)
            {
                return false;
            }

            if (LastControlConnection == null || !LastControlConnection.IsValid)
            {
                return false;
            }

            byte levelIndex = (byte)(value >> 1);
            bool enabled = (value & 1) == 1;
            return TryApplyAccessToggle(LastControlConnection, levelIndex, enabled);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, IdConsoleInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, IdConsoleInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        private bool TryApplyAccessToggle(NetworkConnection conn, byte levelIndex, bool enabled)
        {
            if (!AccessLevelCatalog.TryGetEntry(levelIndex, out AccessLevelEntry entry))
            {
                return false;
            }

            if (!TryResolveOperatorInventory(conn, out HumanInventory operatorInventory))
            {
                PushLog("Operator credential not found.");
                RefreshAllViewers();
                return false;
            }

            IDCard targetCard = GetTargetCard();
            if (targetCard == null)
            {
                PushLog("Insert a target ID card.");
                RefreshAllViewers();
                return false;
            }

            IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();
            AccessMask current = AccessMask.None;
            if (idAccess.TryGetRecord(targetCard.BoundRecordId, out CrewRecord targetRecord))
            {
                current = targetRecord.Access;
            }

            AccessMask updated = enabled
                ? current.With(entry.Level)
                : current.Without(entry.Level);

            if (!idAccess.TrySetTargetAccess(
                    operatorInventory,
                    targetCard,
                    updated,
                    _authLogDevice,
                    out string failureReason))
            {
                PushLog(failureReason);
                RefreshAllViewers();
                return false;
            }

            PushLog($"{(enabled ? "Granted" : "Revoked")} {entry.DisplayName} on {targetCard.OwnerName}.");
            RefreshAllViewers();
            return true;
        }

        private IdConsoleInterfaceSnapshot BuildSnapshot(NetworkConnection conn)
        {
            bool devBypass = IdAccessDevSettings.AllowConsoleAccessEditing;
            bool editorUnlocked = devBypass;
            IDCard targetCard = GetTargetCard();
            AccessMask targetAccess = AccessMask.None;
            string targetName = string.Empty;
            string targetJob = string.Empty;
            bool hasIdAccess = SubSystems.TryGet(out IdAccessSubSystem idAccess);

            if (!devBypass
                && hasIdAccess
                && TryResolveOperatorInventory(conn, out HumanInventory operatorInventory))
            {
                AccessMask changeIdRequired = AccessMask.FromLevels(AccessLevel.ChangeId);
                editorUnlocked = idAccess.CheckAccess(operatorInventory, changeIdRequired, _authLogDevice).Passed;
            }

            if (targetCard != null
                && hasIdAccess
                && idAccess.TryGetRecordView(targetCard.BoundRecordId, out CrewRecord targetRecord))
            {
                targetAccess = targetRecord.Access;
                targetName = targetRecord.Name;
                targetJob = targetRecord.JobName;
            }

            string prompt;
            if (!editorUnlocked)
            {
                prompt = "Present Change ID clearance to unlock editing.";
            }
            else if (targetCard == null)
            {
                prompt = "Insert a target ID card to edit.";
            }
            else if (devBypass)
            {
                prompt = "Debug: edit access levels below.";
            }
            else
            {
                prompt = "Edit access levels below.";
            }

            IdConsoleInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = NetworkObject.ObjectId,
                InterfaceId = InterfaceId,
                Title = _title,
                Subtitle = _subtitle,
                ModelLabel = _modelLabel,
                PromptText = prompt,
                EditorUnlocked = editorUnlocked,
                HasTargetCard = targetCard != null,
                TargetName = targetName,
                TargetJob = targetJob,
                TargetAccessMask = targetAccess.Value,
            };

            snapshot.LogEntryCount = (byte)Mathf.Min(_editLog.Count, IdConsoleInterfaceSnapshot.MaxLogEntries);
            for (int i = 0; i < snapshot.LogEntryCount; i++)
            {
                SetLogEntry(ref snapshot, i, _editLog[i]);
            }

            return snapshot;
        }

        private IDCard GetTargetCard()
        {
            if (_targetCardSlot == null || _targetCardSlot.ItemCount == 0)
            {
                return null;
            }

            foreach (Item item in _targetCardSlot.Items)
            {
                if (item is IDCard idCard)
                {
                    return idCard;
                }
            }

            return null;
        }

        private void PushLog(string message)
        {
            _editLog.Insert(0, message);
            if (_editLog.Count > IdConsoleInterfaceSnapshot.MaxLogEntries)
            {
                _editLog.RemoveAt(_editLog.Count - 1);
            }
        }
    }
}
