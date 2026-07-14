namespace SS3D.Data.Persistence
{
    public sealed class PersistenceContext
    {
        public bool IsTemplateRestore { get; init; }

        public string TemplateName { get; init; }
    }
}
