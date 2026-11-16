using System;
using System.Collections.Generic;

namespace SS3D.Systems.IdAccess
{
    public sealed class DeviceAuthLog
    {
        private readonly AuthLogEntry[] _entries;
        private int _head;
        private int _count;

        public DeviceAuthLog(int capacity = 32)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _entries = new AuthLogEntry[capacity];
        }

        public int Capacity => _entries.Length;

        public int Count => _count;

        public void Append(in AuthLogEntry entry)
        {
            _entries[_head] = entry;
            _head = (_head + 1) % _entries.Length;

            if (_count < _entries.Length)
            {
                _count++;
            }
        }

        public IReadOnlyList<AuthLogEntry> GetEntriesNewestFirst()
        {
            var result = new List<AuthLogEntry>(_count);
            for (int i = 0; i < _count; i++)
            {
                int index = (_head - 1 - i + _entries.Length) % _entries.Length;
                result.Add(_entries[index]);
            }

            return result;
        }
    }
}
