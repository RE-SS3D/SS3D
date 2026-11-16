using System;

namespace SS3D.Systems.IdAccess
{
    public readonly struct AuthLogEntry
    {
        public double Timestamp { get; }

        public string DeviceId { get; }

        public CrewRecordId RequesterRecordId { get; }

        public string RequesterName { get; }

        public AccessMask RequiredAccess { get; }

        public bool Passed { get; }

        public AuthLogEntry(
            double timestamp,
            string deviceId,
            CrewRecordId requesterRecordId,
            string requesterName,
            AccessMask requiredAccess,
            bool passed)
        {
            Timestamp = timestamp;
            DeviceId = deviceId ?? string.Empty;
            RequesterRecordId = requesterRecordId;
            RequesterName = requesterName ?? string.Empty;
            RequiredAccess = requiredAccess;
            Passed = passed;
        }
    }
}
