namespace SS3D.Systems.IdAccess
{
    public readonly struct AccessCheckResult
    {
        public bool Passed { get; }

        public CrewRecordId CredentialRecordId { get; }

        public AccessMask CredentialAccess { get; }

        public AccessCheckFailureReason FailureReason { get; }

        private AccessCheckResult(
            bool passed,
            CrewRecordId credentialRecordId,
            AccessMask credentialAccess,
            AccessCheckFailureReason failureReason)
        {
            Passed = passed;
            CredentialRecordId = credentialRecordId;
            CredentialAccess = credentialAccess;
            FailureReason = failureReason;
        }

        public static AccessCheckResult Pass(CrewRecordId recordId, AccessMask credentialAccess) =>
            new(true, recordId, credentialAccess, AccessCheckFailureReason.None);

        public static AccessCheckResult Fail(AccessCheckFailureReason reason) =>
            new(false, new CrewRecordId(CrewRecordId.None), AccessMask.None, reason);
    }

    public enum AccessCheckFailureReason
    {
        None = 0,
        NoCredential,
        InsufficientAccess,
    }
}
