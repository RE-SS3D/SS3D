using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using UnityEngine;

namespace SS3D.Systems.IdAccess
{
    public sealed class IdAccessSubSystem : NetworkSubSystem
    {
        private readonly Dictionary<CrewRecordId, CrewRecord> _records = new();
        private readonly Dictionary<CrewRecordId, HashSet<IDCard>> _cardsByRecord = new();
        private uint _nextRecordId = 1;

        public event System.Action<CrewRecord> OnCrewRecordChanged;

        [Server]
        public CrewRecord CreateCrewRecord(string name, string jobName, Department department, AccessMask startingAccess)
        {
            var recordId = new CrewRecordId(_nextRecordId++);
            var record = new CrewRecord
            {
                Id = recordId,
                Name = name,
                JobName = jobName,
                Department = department,
                Access = startingAccess,
                ConnectionStatus = CrewConnectionStatus.Online,
            };

            _records[recordId] = record;
            return record;
        }

        [Server]
        public void BindIdCard(IDCard idCard, CrewRecordId recordId)
        {
            if (idCard == null || recordId.IsNone || !_records.TryGetValue(recordId, out CrewRecord record))
            {
                return;
            }

            UnbindIdCard(idCard);
            idCard.ServerBind(record);
            RegisterCard(idCard, recordId);
        }

        [Server]
        public void UnbindIdCard(IDCard idCard)
        {
            if (idCard == null || idCard.BoundRecordId.IsNone)
            {
                return;
            }

            if (_cardsByRecord.TryGetValue(idCard.BoundRecordId, out HashSet<IDCard> cards))
            {
                cards.Remove(idCard);
            }
        }

        [Server]
        public bool TryGetRecord(CrewRecordId recordId, out CrewRecord record)
        {
            return _records.TryGetValue(recordId, out record);
        }

        [Server]
        public bool TrySetAccess(CrewRecordId recordId, AccessMask access)
        {
            if (!_records.TryGetValue(recordId, out CrewRecord record))
            {
                return false;
            }

            record.Access = access;
            RefreshBoundCards(record);
            OnCrewRecordChanged?.Invoke(record);
            return true;
        }

        public bool TryGetRecordView(CrewRecordId recordId, out CrewRecord record)
        {
            return _records.TryGetValue(recordId, out record);
        }

        public AccessCheckResult CheckAccess(
            HumanInventory inventory,
            AccessMask requiredAccess,
            IAuthLogDevice device)
        {
            if (requiredAccess.IsNone)
            {
                return AccessCheckResult.Pass(new CrewRecordId(CrewRecordId.None), AccessMask.None);
            }

            if (!AccessCredentialResolver.TryResolveBoundRecord(inventory, out CrewRecordId recordId, out _))
            {
                var noCredential = AccessCheckResult.Fail(AccessCheckFailureReason.NoCredential);
                AppendAuthLog(device, noCredential, requiredAccess, recordId, string.Empty);
                return noCredential;
            }

            if (!_records.TryGetValue(recordId, out CrewRecord record))
            {
                var noCredential = AccessCheckResult.Fail(AccessCheckFailureReason.NoCredential);
                AppendAuthLog(device, noCredential, requiredAccess, recordId, string.Empty);
                return noCredential;
            }

            bool passed = record.Access.HasAll(requiredAccess);
            var result = passed
                ? AccessCheckResult.Pass(recordId, record.Access)
                : AccessCheckResult.Fail(AccessCheckFailureReason.InsufficientAccess);

            AppendAuthLog(device, result, requiredAccess, recordId, record.Name);
            return result;
        }

        public AccessMask ResolveCredentialMask(HumanInventory inventory)
        {
            if (!AccessCredentialResolver.TryResolveBoundRecord(inventory, out CrewRecordId recordId, out _))
            {
                return AccessMask.None;
            }

            return _records.TryGetValue(recordId, out CrewRecord record)
                ? record.Access
                : AccessMask.None;
        }

        [Server]
        public bool TrySetTargetAccess(
            HumanInventory operatorInventory,
            IDCard targetCard,
            AccessMask newAccess,
            IAuthLogDevice consoleDevice,
            out string failureReason)
        {
            failureReason = string.Empty;
            AccessCheckResult operatorCheck = ResolveConsoleOperatorCheck(
                operatorInventory,
                consoleDevice,
                out failureReason);
            if (!operatorCheck.Passed)
            {
                return false;
            }

            if (targetCard == null || targetCard.BoundRecordId.IsNone)
            {
                failureReason = "No target ID card inserted.";
                return false;
            }

            if (!TryGetRecord(operatorCheck.CredentialRecordId, out CrewRecord operatorRecord)
                || !TryGetRecord(targetCard.BoundRecordId, out CrewRecord targetRecord))
            {
                failureReason = "Could not resolve crew records.";
                return false;
            }

            if (!TrySetAccess(targetCard.BoundRecordId, newAccess))
            {
                failureReason = "Failed to update target access.";
                return false;
            }

            AppendConsoleEditLog(consoleDevice, operatorRecord, targetRecord, newAccess);
            return true;
        }

        public static Department DepartmentForRole(string roleName)
        {
            return roleName switch
            {
                "Security" => Department.Security,
                "Assistant" => Department.Civilian,
                "Captain" => Department.Command,
                "Head of Personnel" => Department.Command,
                _ => Department.None,
            };
        }

        private void RegisterCard(IDCard idCard, CrewRecordId recordId)
        {
            if (!_cardsByRecord.TryGetValue(recordId, out HashSet<IDCard> cards))
            {
                cards = new HashSet<IDCard>();
                _cardsByRecord[recordId] = cards;
            }

            cards.Add(idCard);
        }

        private void RefreshBoundCards(CrewRecord record)
        {
            if (!_cardsByRecord.TryGetValue(record.Id, out HashSet<IDCard> cards))
            {
                return;
            }

            foreach (IDCard card in cards)
            {
                if (card != null)
                {
                    card.ServerRefreshView(record);
                }
            }
        }

        private AccessCheckResult ResolveConsoleOperatorCheck(
            HumanInventory operatorInventory,
            IAuthLogDevice consoleDevice,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (IdAccessDevSettings.AllowConsoleAccessEditing)
            {
                if (!AccessCredentialResolver.TryResolveBoundRecord(
                        operatorInventory,
                        out CrewRecordId operatorRecordId,
                        out _))
                {
                    failureReason = "Operator credential not found.";
                    return AccessCheckResult.Fail(AccessCheckFailureReason.NoCredential);
                }

                AccessMask operatorAccess = ResolveCredentialMask(operatorInventory);
                return AccessCheckResult.Pass(operatorRecordId, operatorAccess);
            }

            AccessMask changeIdRequired = AccessMask.FromLevels(AccessLevel.ChangeId);
            AccessCheckResult operatorCheck = CheckAccess(operatorInventory, changeIdRequired, consoleDevice);
            if (!operatorCheck.Passed)
            {
                failureReason = "Operator lacks Change ID access.";
            }

            return operatorCheck;
        }

        private static void AppendAuthLog(
            IAuthLogDevice device,
            AccessCheckResult result,
            AccessMask requiredAccess,
            CrewRecordId recordId,
            string requesterName)
        {
            if (device?.AuthLog == null)
            {
                return;
            }

            device.AuthLog.Append(new AuthLogEntry(
                Time.timeAsDouble,
                device.DeviceId,
                recordId,
                requesterName,
                requiredAccess,
                result.Passed));
        }

        private static void AppendConsoleEditLog(
            IAuthLogDevice device,
            CrewRecord operatorRecord,
            CrewRecord targetRecord,
            AccessMask newAccess)
        {
            if (device?.AuthLog == null)
            {
                return;
            }

            device.AuthLog.Append(new AuthLogEntry(
                Time.timeAsDouble,
                device.DeviceId,
                operatorRecord.Id,
                operatorRecord.Name,
                newAccess,
                passed: true));
        }
    }
}
