using System.Collections.Generic;

namespace SS3D.Systems.Area
{
    public sealed class AreaRegistry
    {
        private readonly Dictionary<ushort, AreaRecord> _byId = new();
        private readonly Dictionary<IAreaApcOrigin, AreaId> _byApc = new();
        private readonly List<AreaRecord> _allAreasCache = new();
        private bool _allAreasDirty = true;
        private ushort _nextId = 1;

        public IReadOnlyDictionary<ushort, AreaRecord> ById => _byId;

        public void Clear()
        {
            _byId.Clear();
            _byApc.Clear();
            _allAreasCache.Clear();
            _allAreasDirty = false;
            _nextId = 1;
        }

        public AreaId AllocateId()
        {
            for (int i = 0; i < ushort.MaxValue; i++)
            {
                ushort candidate = _nextId++;
                if (candidate != AreaId.None && !_byId.ContainsKey(candidate)) return new AreaId(candidate);
            }
            throw new System.InvalidOperationException("No area ids remain.");
        }

        public void RefreshApcLookup()
        {
            _byApc.Clear();
            foreach (AreaRecord record in _byId.Values)
                if (record.Apc != null) _byApc[record.Apc] = record.Id;
        }

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
            _allAreasDirty = true;
        }

        public void Unregister(AreaId areaId)
        {
            if (areaId.IsNone || !_byId.TryGetValue(areaId.Value, out AreaRecord record))
                return;

            if (record.Apc != null)
                _byApc.Remove(record.Apc);

            _byId.Remove(areaId.Value);
            _allAreasDirty = true;
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

        /// <summary>
        /// Live snapshot of registered areas. Cached until Register/Unregister/Clear;
        /// do not mutate the returned list.
        /// </summary>
        public IReadOnlyList<AreaRecord> GetAllAreas()
        {
            if (!_allAreasDirty)
            {
                return _allAreasCache;
            }

            _allAreasCache.Clear();
            foreach (AreaRecord record in _byId.Values)
            {
                _allAreasCache.Add(record);
            }

            _allAreasDirty = false;
            return _allAreasCache;
        }
    }
}
