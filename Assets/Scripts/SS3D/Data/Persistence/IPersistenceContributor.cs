namespace SS3D.Data.Persistence
{
    public interface IPersistenceContributor
    {
        string ContributorId { get; }

        PersistenceLayer Layer { get; }

        int LoadOrder { get; }

        object Capture();

        void Restore(object data, PersistenceContext context);
    }
}
