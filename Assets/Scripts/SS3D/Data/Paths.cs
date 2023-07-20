using JetBrains.Annotations;
using SS3D.Logging;
using System;
using System.IO;
using UnityEngine;

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
		private static string GamePath => Application.isEditor ? EditorGameFilePath : BuiltGameFilePath;
		
		/// <summary>
		/// Gets the full path to the application folder.
		/// </summary>
		[NotNull]
		private static string FullGamePath => Path.GetFullPath(".");

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

		/// <summary>
		/// Reads or creates a file.
		/// </summary>
		/// <param name="path">The full path including the file.</param>
		/// <returns>The read or created file.</returns>
		[NotNull]
		public static string ReadOrCreateIfNotExistsFile([NotNull] string path) 
		{
			if (File.Exists(path))
			{
				return File.ReadAllText(path);
			}

			Log.Information(nameof(Paths), $"File {path} not found, creating a new one.");
			
			File.Create(path);
			return File.ReadAllText(path);
		}

		/// <summary>
		/// Writes or creates a/to a file.
		/// </summary>
		/// <param name="path">File path.</param>
		/// <param name="fileContent">File content.</param>
		public static void WriteOrCreateIfNotExistsFile([NotNull] string path, string fileContent)
		{
			if (!File.Exists(path))
			{
				File.Create(path);
			}

			File.WriteAllText(path, fileContent);
		}
	}
}