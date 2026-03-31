using JetBrains.Annotations;

namespace SS3D.Data
{
    /// <summary>
    /// Contract for GUID to backend-specific key resolution.
    /// Each concrete database holds its own serialized mapping.
    /// </summary>
    public interface IAssetDatabase
    {
        /// <summary>
        /// The ID of the database with which to identify it.
        /// </summary>
        string DatabaseID { get; }

        /// <summary>
        /// Returns true if this database contains the given GUID.
        /// </summary>
        bool Has([NotNull] string guid);

        /// <summary>
        /// Resolves a GUID to the backend-specific key used for loading.
        /// </summary>
        [NotNull]
        string ResolveKey([NotNull] string guid);
    }
}