using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Mutation-driven <c>areaId → vents/scrubbers</c> index. Membership is a pure function of
    /// the flooded tilemap; rebuild on port register/unregister or area reflood — never scan
    /// the full port registry per UI/preset query.
    /// </summary>
    public sealed class AtmosAreaPortIndex
    {
        private static readonly IReadOnlyList<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> Empty =
            Array.Empty<(AtmosPortControllerBase, AtmosAreaPortKind)>();

        private readonly Dictionary<AreaId, List<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)>> _portsByArea =
            new();

        private bool _dirty = true;

        public void Invalidate() => _dirty = true;

        public void EnsureBuilt(AtmosPortRegistry registry, AreaSubSystem areaSubSystem)
        {
            if (!_dirty)
            {
                return;
            }

            Rebuild(registry, areaSubSystem);
            _dirty = false;
        }

        public IReadOnlyList<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> GetPortsInArea(AreaId areaId)
        {
            if (areaId.IsNone)
            {
                return Empty;
            }

            return _portsByArea.TryGetValue(areaId, out List<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> list)
                ? list
                : Empty;
        }

        public void ForEachPortInArea(AreaId areaId, Action<AtmosPortControllerBase, AtmosAreaPortKind> action)
        {
            if (action == null || areaId.IsNone)
            {
                return;
            }

            IReadOnlyList<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> ports = GetPortsInArea(areaId);
            for (int i = 0; i < ports.Count; i++)
            {
                (AtmosPortControllerBase port, AtmosAreaPortKind kind) = ports[i];
                if (port != null)
                {
                    action(port, kind);
                }
            }
        }

        private void Rebuild(AtmosPortRegistry registry, AreaSubSystem areaSubSystem)
        {
            foreach (KeyValuePair<AreaId, List<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)>> entry in _portsByArea)
            {
                entry.Value.Clear();
            }

            if (registry == null || areaSubSystem == null)
            {
                return;
            }

            registry.ForEachPort(port =>
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
                    || record.Id.IsNone)
                {
                    return;
                }

                if (!_portsByArea.TryGetValue(record.Id, out List<(AtmosPortControllerBase Port, AtmosAreaPortKind Kind)> list))
                {
                    list = new List<(AtmosPortControllerBase, AtmosAreaPortKind)>();
                    _portsByArea[record.Id] = list;
                }

                list.Add((controller, kind.Value));
            });
        }
    }
}
