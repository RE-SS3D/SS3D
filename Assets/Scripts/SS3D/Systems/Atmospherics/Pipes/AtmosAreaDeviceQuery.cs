using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Area;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    public enum AtmosAreaPortKind
    {
        Vent = 0,
        Scrubber = 1,
    }

    public readonly struct AtmosAreaPortRecord
    {
        public readonly int ObjectId;
        public readonly AtmosAreaPortKind Kind;
        public readonly string Name;
        public readonly bool Powered;
        public readonly float TargetKpa;
        public readonly bool FilterO2;
        public readonly bool FilterN2;
        public readonly bool FilterCo2;
        public readonly bool FilterToxins;

        public AtmosAreaPortRecord(
            int objectId,
            AtmosAreaPortKind kind,
            string name,
            bool powered,
            float targetKpa,
            bool filterO2,
            bool filterN2,
            bool filterCo2,            bool filterToxins)
        {
            ObjectId = objectId;
            Kind = kind;
            Name = name;
            Powered = powered;
            TargetKpa = targetKpa;
            FilterO2 = filterO2;
            FilterN2 = filterN2;
            FilterCo2 = filterCo2;
            FilterToxins = filterToxins;
        }
    }

    /// <summary>
    /// Discovers vents and scrubbers that belong to an APC flood-filled area.
    /// Uses <see cref="AtmosSubSystem"/>'s mutation-driven area→port index — does not
    /// scan the full port registry or re-resolve area membership per query.
    /// </summary>
    public static class AtmosAreaDeviceQuery
    {
        public static bool TryCollectAreaPorts(AreaId areaId, List<AtmosAreaPortRecord> results)
        {
            if (results == null || areaId.IsNone)
            {
                return false;
            }

            results.Clear();
            if (!SubSystems.TryGet(out AtmosSubSystem atmosSubSystem)
                || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return false;
            }

            IReadOnlyList<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> ports =
                atmosSubSystem.GetIndexedPortsForArea(areaId);

            string areaDisplayName = null;
            if (areaSubSystem.TryGetArea(areaId, out AreaRecord areaRecord))
            {
                areaDisplayName = areaRecord.DisplayName;
            }

            for (int i = 0; i < ports.Count; i++)
            {
                (AtmosPortControllerBase controller, AtmosAreaPortKind kind) = ports[i];
                if (controller == null
                    || controller is not NetworkBehaviour networkBehaviour
                    || networkBehaviour.NetworkObject == null)
                {
                    continue;
                }

                results.Add(BuildRecord(
                    controller,
                    kind,
                    networkBehaviour.NetworkObject.ObjectId,
                    areaDisplayName));
            }

            results.Sort(CompareRecords);
            return results.Count > 0;
        }

        public static void ForEachAreaPort(AreaId areaId, Action<AtmosPortControllerBase, AtmosAreaPortKind> action)
        {
            if (action == null || areaId.IsNone)
            {
                return;
            }

            if (!SubSystems.TryGet(out AtmosSubSystem atmosSubSystem))
            {
                return;
            }

            IReadOnlyList<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> ports =
                atmosSubSystem.GetIndexedPortsForArea(areaId);

            for (int i = 0; i < ports.Count; i++)
            {
                (AtmosPortControllerBase controller, AtmosAreaPortKind kind) = ports[i];
                if (controller != null)
                {
                    action(controller, kind);
                }
            }
        }

        public static bool TryResolvePort(int objectId, out AtmosPortControllerBase controller, out AtmosAreaPortKind kind)
        {
            controller = null;
            kind = default;

            if (!SubSystems.TryGet(out AtmosSubSystem atmosSubSystem))
            {
                return false;
            }

            AtmosPortControllerBase resolvedController = null;
            AtmosAreaPortKind resolvedKind = default;
            bool found = false;

            atmosSubSystem.PortRegistry.ForEachPort(port =>
            {
                if (found || port is not AtmosPortControllerBase candidate)
                {
                    return;
                }

                if (candidate is not NetworkBehaviour networkBehaviour
                    || networkBehaviour.NetworkObject == null
                    || networkBehaviour.NetworkObject.ObjectId != objectId)
                {
                    return;
                }

                if (candidate is not VentController and not ScrubberController)
                {
                    return;
                }

                resolvedKind = candidate is VentController
                    ? AtmosAreaPortKind.Vent
                    : AtmosAreaPortKind.Scrubber;
                resolvedController = candidate;
                found = true;
            });

            if (!found)
            {
                return false;
            }

            controller = resolvedController;
            kind = resolvedKind;
            return true;
        }

        private static AtmosAreaPortRecord BuildRecord(
            AtmosPortControllerBase controller,
            AtmosAreaPortKind kind,
            int objectId,
            string areaDisplayName)
        {
            bool isVent = kind == AtmosAreaPortKind.Vent;
            float targetKpa = 101f;
            if (isVent && controller is VentController vent)
            {
                targetKpa = vent.TargetPressureKpa;
            }

            bool filterO2 = false;
            bool filterN2 = false;
            bool filterCo2 = true;            bool filterToxins = false;
            if (!isVent && controller.TryGetComponent(out ScrubberController scrubber))
            {
                scrubber.GetFilterStates(
                    out filterO2,
                    out filterN2,
                    out filterCo2,                    out filterToxins);
            }

            return new AtmosAreaPortRecord(
                objectId,
                kind,
                FormatDeviceName(isVent, areaDisplayName),
                controller.IsEnabled,
                targetKpa,
                filterO2,
                filterN2,
                filterCo2,
                filterToxins);
        }

        private static string FormatDeviceName(bool isVent, string areaDisplayName)
        {
            string kind = isVent ? "Vent" : "Scrubber";
            if (!string.IsNullOrWhiteSpace(areaDisplayName))
            {
                return $"{kind} — {areaDisplayName}";
            }

            return kind;
        }

        private static int CompareRecords(AtmosAreaPortRecord left, AtmosAreaPortRecord right)
        {
            int byId = left.ObjectId.CompareTo(right.ObjectId);
            return byId != 0 ? byId : string.CompareOrdinal(left.Name, right.Name);
        }
    }
}
