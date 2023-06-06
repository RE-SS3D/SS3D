namespace SS3D.Core.Utils
{
	/// <summary>
	/// Stores all the constants for CMD arguments for easy access and readability.
	/// For bool args simply add the arg.
	/// </summary>
	public static class CommandLineArgs
	{
		/// <summary>
		/// The "-serveronly" arg in the executable.
		/// </summary>
		public const string ServerOnly = "-serveronly";
		/// <summary>
		/// The "-host" arg in the executable.
		/// </summary>
		public const string Host = "-host";
		/// <summary>
		/// The ip to connect to. String.
		/// </summary>
		public const string Ip = "-ip=";
		/// <summary>
		/// The server port to connect to or host. String.
		/// </summary>
		public const string Port = "-port=";
		/// <summary>
		/// String.
		/// This is temporary, in production use, this will not exist,
		/// and be replaced by the token, and then the server will get the Username.
		/// </summary>
		public const string Ckey = "-ckey=";
		/// <summary>
		/// String.
		/// in production this will be sent by the Hub to the client executable.
		/// </summary>
		public const string AccessToken = "-token=";
		/// <summary>
		/// Skips the intro.
		/// </summary>
		public const string SkipIntro = "-skipintro";
		/// <summary>
		/// Disables the discord integration.
		/// </summary>
		public const string EnableDiscordIntegration = "-enablediscordintegration";
		/// <summary>
		/// Forces the launcher to be initialized. 
		/// </summary>
		public const string ForceLauncher = "-forceLauncher";
	}
}