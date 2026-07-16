using JetBrains.Annotations;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SS3D.Data.Management
{
    /// <summary>
    /// Class for the saving and loading of serialized objects. Uses Generics and can be adapted for any serializable object.
    /// </summary>
    public static class LocalStorage
    {
		/// <summary>
		/// The saved files extension. 
		/// </summary>
        private const string SaveExtension = "json";

		/// <summary>
		/// Folder where to save the save files.
		/// </summary>
        private static readonly string SaveFolder = Paths.GetPath(GamePaths.Data, true);

        private static bool IsInitialized;

        public static void Initialize()
        {
	        if (IsInitialized)
	        {
		        return;
	        }

	        IsInitialized = true;

	        if (!Directory.Exists(SaveFolder))
	        {
		        Directory.CreateDirectory(SaveFolder);
	        }
        }

        private static bool Save(string fileName, string saveString, bool overwrite)
        {
            Initialize();

            string saveFileName = fileName;

            if (!overwrite)
            {
                // Make sure the Save Number is unique so it doesnt overwrite a previous save file
                int saveNumber = 1;
                while (File.Exists(SaveFolder + saveFileName + "." + SaveExtension))
                {
                    saveNumber++;
                    saveFileName = fileName + "_" + saveNumber;
                }
            }

            try
            {
                string fullPath = SaveFolder + saveFileName + "." + SaveExtension;
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

	            File.WriteAllText(fullPath, saveString);
	            Log.Debug(typeof(LocalStorage), $"Saved file {fileName}");
            }
            catch (Exception e)
            {
	            Log.Error(typeof(LocalStorage), $"Something went wrong when saving {fileName}: {e.Message}");

	            return false;
            }

            return true;
        }

        private static bool TryGetDirectoryInfo(string path, out DirectoryInfo directoryInfo)
        {
            Initialize();

            string fullPath = SaveFolder + path;
            if (!Directory.Exists(fullPath))
            {
                directoryInfo = null;
                return false;
            }

            directoryInfo = new DirectoryInfo(fullPath);
            return true;
        }

        private static bool Load(string fileName, [CanBeNull] out string saveString)
        {
	        if (!File.Exists(SaveFolder + fileName + "." + SaveExtension))
	        {
		        saveString = null;
		        return false;
	        }

	        saveString = File.ReadAllText(SaveFolder + fileName + "." + SaveExtension);
	        return true;

        }

        private static bool TryLoadMostRecentFile(string path, [CanBeNull] out string file)
        {
            if (!TryGetDirectoryInfo(path, out DirectoryInfo directoryInfo))
            {
                file = null;
                return false;
            }

            // Get all save files
            FileInfo[] saveFiles = directoryInfo.GetFiles("*." + SaveExtension);

            // Cycle through all save files and identify the most recent one
            FileInfo mostRecentFile = null;
            foreach (FileInfo fileInfo in saveFiles)
            {
                if (mostRecentFile == null)
                {
                    mostRecentFile = fileInfo;
                }
                else
                {
                    if (fileInfo.LastWriteTime > mostRecentFile.LastWriteTime)
                    {
                        mostRecentFile = fileInfo;
                    }
                }
            }

            // If theres a save file, load it, if not return null
            if (mostRecentFile == null)
            {
	            file = null;
	            return false;
            }

            Log.Debug(typeof(LocalStorage), $"Loaded the the most recent file at {path}");

            file = File.ReadAllText(mostRecentFile.FullName);
            return true;

        }

        public static bool SaveObject(string fileName, object saveObject, bool overwrite = false)
        {
            string json = JsonUtility.ToJson(saveObject);
            
            return Save(fileName, json, overwrite);
        }

        /// <summary>
        /// Rename a file.
        /// </summary>
        /// <param name="oldFileName"> The old file full path, not including extension.</param>
        /// <param name="newFileName"> The new file full path, not including extension.</param>
        public static void RenameFile(string oldFileName, string newFileName)
        {
            File.Move(SaveFolder + oldFileName + "." + SaveExtension, SaveFolder + newFileName + "." + SaveExtension);
        }

        public static void DeleteFile(string fileName)
        {
            File.Delete(SaveFolder + fileName + "." + SaveExtension);
        }

        public static TSaveObject LoadObject<TSaveObject>(string fileName)
        {
	        if (Load(fileName, out string saveString))
	        {
		        TSaveObject saveObject = JsonUtility.FromJson<TSaveObject>(saveString);
		        return saveObject;
	        }

	        return default;
        }

        public static bool TryReadRaw(string fileName, out string saveString)
        {
            return Load(fileName, out saveString);
        }

        public static DateTime GetLastWriteTimeUtc(string fileName)
        {
            string fullPath = SaveFolder + fileName + "." + SaveExtension;
            return File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath) : DateTime.MinValue;
        }

        public static TSaveObject LoadMostRecentObject<TSaveObject>(string path)
        {
            bool mostRecentFileExists = TryLoadMostRecentFile(path, out string saveString);

            if (!mostRecentFileExists)
            {
                return default;
            }

            TSaveObject saveObject = JsonUtility.FromJson<TSaveObject>(saveString);
            return saveObject;

        }

        public static string GetMostRecentFileName(string path)
        {
            if (!TryGetDirectoryInfo(path, out DirectoryInfo directoryInfo))
            {
                return null;
            }

            FileInfo[] saveFiles = directoryInfo.GetFiles("*." + SaveExtension);
            FileInfo mostRecentFile = null;

            foreach (FileInfo fileInfo in saveFiles)
            {
                if (mostRecentFile == null || fileInfo.LastWriteTime > mostRecentFile.LastWriteTime)
                {
                    mostRecentFile = fileInfo;
                }
            }

            if (mostRecentFile == null)
            {
                return null;
            }

            string fileName = mostRecentFile.Name;
            return fileName.EndsWith("." + SaveExtension)
                ? fileName[..^(SaveExtension.Length + 1)]
                : fileName;
        }

        /// <summary>
        /// Get the name of all files in a given folder.
        /// </summary>
        /// <param name="path"> Full path to the folder.</param>
        /// <returns></returns>
        public static List<string> GetAllObjectsNameInFolder(string path)
        {
            var allFileNames = new List<string>();

            if (!TryGetDirectoryInfo(path, out DirectoryInfo directoryInfo))
            {
                return allFileNames;
            }

            // Get all save files
            FileInfo[] saveFiles = directoryInfo.GetFiles("*." + SaveExtension);

            foreach (FileInfo fileInfo in saveFiles)
            {
                allFileNames.Add(fileInfo.Name);
            }

            return allFileNames;
        }

        /// <summary>
        /// Checks if a file is already present in a given folder.
        /// </summary>
        /// <param name="folderPath"> Full path to the folder.</param>
        /// <param name="name"> Name of the file (without the extension).</param>
        /// <returns></returns>
        public static bool FolderAlreadyContainsName(string folderPath, string name)
        {
            return GetAllObjectsNameInFolder(folderPath).Contains(name + "." + SaveExtension);
        }

        public static bool AppendLine(string fileName, string line)
        {
            return AppendToFile(fileName, SaveExtension, line);
        }

        public static bool AppendJsonlLine(string fileName, string line)
        {
            return AppendToFile(fileName, "jsonl", line);
        }

        private static bool AppendToFile(string fileName, string extension, string line)
        {
            Initialize();

            try
            {
                string fullPath = SaveFolder + fileName + "." + extension;
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(fullPath, line + Environment.NewLine);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(typeof(LocalStorage), $"Something went wrong when appending to {fileName}: {e.Message}");
                return false;
            }
        }
    }
}
