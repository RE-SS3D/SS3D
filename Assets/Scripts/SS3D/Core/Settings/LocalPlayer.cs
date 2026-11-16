namespace SS3D.Core.Settings
{
    /// <summary>
    /// Holds data from the Hub to send the server when we connect
    /// </summary>
    public static class LocalPlayer
    {
        /// <summary>
        /// Unique client key, originally used in BYOND's user management, nostalgically used
        /// </summary>
        public static string Ckey { get; private set; }

        public static void UpdateCkey(string ckey)
        {
            Ckey = ckey;
        }

        /// <summary>
        /// Secret from <c>-token=</c>. Sent at login; a dedicated server only grants an admin
        /// name when it matches that name's line in <c>Config/admin-tokens.txt</c>.
        /// </summary>
        public static string AccessToken { get; private set; }

        public static void UpdateAccessToken(string token)
        {
            AccessToken = token;
        }
    }
}
