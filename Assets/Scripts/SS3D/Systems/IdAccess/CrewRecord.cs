namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Server-side identity rail entry for one active crew member this round.
    /// </summary>
    public sealed class CrewRecord
    {
        public CrewRecordId Id { get; set; }

        public string Name { get; set; }

        public string JobName { get; set; }

        public Department Department { get; set; }

        public AccessMask Access { get; set; }

        /// <summary>Stub until death-cloning-respawn ships.</summary>
        public uint? DnaRecordId { get; set; }

        public CrewConnectionStatus ConnectionStatus { get; set; } = CrewConnectionStatus.Online;
    }
}
