using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
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
        public readonly bool FilterPlasma;
        public readonly bool FilterToxins;

        public AtmosAreaPortRecord(
            int objectId,
            AtmosAreaPortKind kind,
            string name,
            bool powered,
            float targetKpa,
            bool filterO2,
            bool filterN2,
            bool filterCo2,
            bool filterPlasma,
            bool filterToxins)
        {
            ObjectId = objectId;
            Kind = kind;
            Name = name;
            Powered = powered;
            TargetKpa = targetKpa;
            FilterO2 = filterO2;
            FilterN2 = filterN2;
            FilterCo2 = filterCo2;
            FilterPlasma = filterPlasma;
            FilterToxins = filterToxins;
        }
    }

    /// <summary>
    /// Discovers vents and scrubbers that belong to an APC flood-filled area.
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

            atmosSubSystem.PortRegistry.ForEachPort(port =>
            {
                if (port is not AtmosPortControllerBase controller)
                {
                    return;
                }

                if (controller is not VentController and not ScrubberController)
                {
                    return;
                }

                if (controller is not NetworkBehaviour networkBehaviour
                    || networkBehaviour.NetworkObject == null)
                {
                    return;
                }

                if (!controller.TryGetComponent(out PlacedTileObject tileObject)
                    || !areaSubSystem.TryGetAreaForDevice(tileObject, out AreaRecord record)
                    || record.Id != areaId)
                {
                    return;
                }

                results.Add(BuildRecord(controller, networkBehaviour.NetworkObject.ObjectId, record.DisplayName));
            });

            results.Sort(CompareRecords);
            return results.Count > 0;
        }

        public static void ForEachAreaPort(AreaId areaId, Action<AtmosPortControllerBase, AtmosAreaPortKind> action)
        {
            if (action == null || areaId.IsNone)
            {
                return;
            }

            if (!SubSystems.TryGet(out AtmosSubSystem atmosSubSystem)
                || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            atmosSubSystem.PortRegistry.ForEachPort(port =>
            {
                if (port is not AtmosPortControllerBase controller)
                {
                    return;
                }

                AtmosAreaPortKind? kind = controller switch
                {
                    VentController => AtmosAreaPortKind.Vent,
                    ScrubberController => AtmosAreaPortKind.Scrubber,
                    _ => null,
                };

                if (kind == null)
                {
                    return;
                }

                if (!controller.TryGetComponent(out PlacedTileObject tileObject)
                    || !areaSubSystem.TryGetAreaForDevice(tileObject, out AreaRecord record)
                    || record.Id != areaId)
                {
                    return;
                }

                action(controller, kind.Value);
            });
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
            int objectId,
            string areaDisplayName)
        {
            bool isVent = controller is VentController;
            float targetKpa = 101f;
            if (controller is VentController vent)
            {
                targetKpa = vent.TargetPressureKpa;
            }

            bool filterO2 = true;
            bool filterN2 = true;
            bool filterCo2 = true;
            bool filterPlasma = false;
            bool filterToxins = true;
            if (!isVent && controller.TryGetComponent(out ScrubberController scrubber))
            {
                scrubber.GetFilterStates(
                    out filterO2,
                    out filterN2,
                    out filterCo2,
                    out filterPlasma,
                    out filterToxins);
            }

            return new AtmosAreaPortRecord(
                objectId,
                isVent ? AtmosAreaPortKind.Vent : AtmosAreaPortKind.Scrubber,
                FormatDeviceName(isVent, areaDisplayName),
                controller.IsEnabled,
                targetKpa,
                filterO2,
                filterN2,
                filterCo2,
                filterPlasma,
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
