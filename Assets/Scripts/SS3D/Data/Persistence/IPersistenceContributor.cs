namespace SS3D.Data.Persistence
{
    public interface IPersistenceContributor
    {
        string ContributorId { get; }

        PersistenceLayer Layer { get; }

        int LoadOrder { get; }

        object Capture();

        /// <summary>Reads this contributor's saved payload back into the object <see cref="Restore"/> takes.</summary>
        object DeserializePayload(string payloadJson);

        void Restore(object data, PersistenceContext context);
    }
}
