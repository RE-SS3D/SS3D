using JetBrains.Annotations;
using System.IO;

#if UNITY_EDITOR
using ParrelSync;
#endif

namespace SS3D.Data
{
	/// <summary>
	/// Class used to get game paths.
	/// </summary>
	public static class Paths
	{
		/// <summary>
		/// Gets the config path from the root path, excluding everything outside the game root folder.
		/// </summary>
		[NotNull]
		private static string GamePath => UnityEngine.Application.isEditor ? EditorGameFilePath : BuiltGameFilePath;
		
		/// <summary>
		/// Gets the full path to the application folder.
		/// </summary>
		[NotNull]
		private static string FullGamePath =>
#if UNITY_EDITOR
            ClonesManager.IsClone() ? ClonesManager.GetOriginalProjectPath() : Path.GetFullPath(".");
#else
            _executableFolder ??= ExecutableFolder();
#endif

#if !UNITY_EDITOR
		/// <summary>
		/// The folder holding the executable. Config/ and Data/ ship beside it, so a player started from
		/// another working directory (e.g. <c>./Game/SS3D-Client</c>) still finds its maps and permissions.
		/// </summary>
		private static string _executableFolder;

		/// <summary>Application.dataPath is main-thread only, so resolve it before any scene loads.</summary>
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void CacheExecutableFolder() => _executableFolder = ExecutableFolder();

		// dataPath is <folder>/<product>_Data on Windows and Linux players.
		private static string ExecutableFolder() =>
			Path.GetDirectoryName(Path.GetFullPath(UnityEngine.Application.dataPath)) ?? Path.GetFullPath(".");
#endif

		/// <summary>
		/// The path to the Config folder on the Editor project.
		/// </summary>
		private const string EditorGameFilePath = "/Builds/Game";
		/// <summary>
		/// The path to the config folder on the built project.
		/// </summary>
		private const string BuiltGameFilePath = "";

		/// <summary>
		/// Gets a path in the game folder.
		/// </summary>
		/// <param name="gamePath">What path you want.</param>
		/// <param name="fullPath">If you'll get the full path with all the folders.</param>
		/// <returns>The path relating to the gamePath.</returns>
		[NotNull]
		public static string GetPath(GamePaths gamePath, bool fullPath = false)
        {
			string path = (fullPath ? FullGamePath : string.Empty) + GamePath + "/" + gamePath;

			return path;
		}
	}
}