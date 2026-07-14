using System.Collections.Generic;

namespace SS3D.Data.Persistence
{
    public interface IPersistenceStore
    {
        bool TrySave(string path, PersistenceEnvelope envelope, bool overwrite);

        bool TryLoad(string path, out PersistenceEnvelope envelope);

        IReadOnlyList<string> List(string directory);
    }
}
