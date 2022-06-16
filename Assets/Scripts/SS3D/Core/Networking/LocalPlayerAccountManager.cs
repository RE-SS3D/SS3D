namespace SS3D.Core.Networking
{
    /// <summary>
    /// Holds data from the Hub to send the server when we connect
    /// </summary>
    public static class LocalPlayerAccountManager
    {
        /// <summary>
        /// Unique client key, originally used in BYOND's user management, nostalgically used
        /// </summary>
        public static string Ckey { get; private set; }

        public static void UpdateCkey(string ckey)
        {
            Ckey = ckey; 
        }

    }
}
