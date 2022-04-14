namespace SS3D.Core.Networking.Utils
{
    /// <summary>
    /// Stores all the constants for CMD arguments for easy access and readability   
    /// </summary>
    public static class CommandLineArgs
    {
        /// <summary>
        /// The "-host" arg in the executable, should be followed by a bool.
        /// </summary>
        public const string Host = "-host";
        /// <summary>
        /// String.
        /// </summary>
        public const string Ip = "-ip=";
        /// <summary>
        /// String.
        /// This is temporary, in production use, this will not exist,
        /// and be replaced by the token, and then the server will get the
        /// Username.
        /// </summary>
        public const string Ckey = "-ckey=";
        /// <summary>
        /// String.
        /// in production this will be sent by the Hub to the client executable.
        /// </summary>
        public const string AccessToken = "-token=";
        /// <summary>
        /// Bool.
        /// Skips the intro
        /// </summary>
        public const string SkipIntro = "-skipintro";
    }
}