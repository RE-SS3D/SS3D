using System.Collections.Generic;

namespace SS3D.Systems.Area
{
    public sealed class AreaRegistry
    {
        private readonly Dictionary<ushort, AreaRecord> _byId = new();
        private readonly Dictionary<IAreaApcOrigin, AreaId> _byApc = new();
        private ushort _nextId = 1;

        public IReadOnlyDictionary<ushort, AreaRecord> ById => _byId;

        public void Clear()
        {
            _byId.Clear();
            _byApc.Clear();
            _nextId = 1;
        }

        public AreaId AllocateId() => new AreaId(_nextId++);

        public void EnsureNextIdAbove(ushort highestUsedId)
        {
            if (highestUsedId >= _nextId)
                _nextId = (ushort)(highestUsedId + 1);
        }

        public void Register(AreaRecord record)
        {
            _byId[record.Id.Value] = record;
            if (record.Apc != null)
                _byApc[record.Apc] = record.Id;
        }

        public void Unregister(AreaId areaId)
        {
            if (areaId.IsNone || !_byId.TryGetValue(areaId.Value, out AreaRecord record))
                return;

            if (record.Apc != null)
                _byApc.Remove(record.Apc);

            _byId.Remove(areaId.Value);
        }

        public bool TryGet(AreaId areaId, out AreaRecord record)
        {
            if (areaId.IsNone)
            {
                record = null;
                return false;
            }

            return _byId.TryGetValue(areaId.Value, out record);
        }

        public bool TryGetApcArea(IAreaApcOrigin apc, out AreaId areaId)
        {
            if (apc == null)
            {
                areaId = default;
                return false;
            }

            return _byApc.TryGetValue(apc, out areaId);
        }

        public IReadOnlyList<AreaRecord> GetAllAreas()
        {
            var list = new List<AreaRecord>(_byId.Count);
            foreach (AreaRecord record in _byId.Values)
                list.Add(record);

            return list;
        }
    }
}
