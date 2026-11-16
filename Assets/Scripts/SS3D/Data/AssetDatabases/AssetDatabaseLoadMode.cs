namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// How an <see cref="AssetDatabase"/> resolves assets at runtime.
    /// </summary>
    public enum AssetDatabaseLoadMode
    {
        /// <summary>
        /// Editor copies Addressables group entries into the serialized <see cref="AssetDatabase.Assets"/>
        /// dictionary; runtime <c>Get</c> is a sync dictionary read (eager RAM residency).
        /// </summary>
        EagerSerialized = 0,

        /// <summary>
        /// Runtime loads by GUID via Addressables (<see cref="AssetProvider"/>). Serialized
        /// <see cref="AssetDatabase.Assets"/> stays empty; <see cref="AssetDatabase.AssetKeys"/> lists GUIDs.
        /// </summary>
        AddressablesAsync = 1,
    }
}
