using Codice.Utils;
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
    public static class SaveSystem
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
	            File.WriteAllText(SaveFolder + saveFileName + "." + SaveExtension, saveString);
	            Log.Information(typeof(SaveSystem), $"Saved file {fileName}");
            }
            catch (Exception e)
            {
	            Log.Error(typeof(SaveSystem), $"Something went wrong when saving {fileName}: {e.Message}");

	            return false;
            }

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
            DirectoryInfo directoryInfo = new(SaveFolder + path);

            if (!Directory.Exists(SaveFolder))
            {
				Log.Information(nameof(SaveSystem), $"No saves found, creating new folder at {SaveFolder}");

				Directory.CreateDirectory(SaveFolder);
            }

            // Get all save files
            FileInfo[] saveFiles = directoryInfo.GetFiles( "*." + SaveExtension);

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
				Log.Error(typeof(SaveSystem), $"Failed to find the most recent file at {path}");

	            file = null;
	            return false;
            }

            Log.Information(typeof(SaveSystem), $"Loaded the the most recent file at {path}");

            file = File.ReadAllText(mostRecentFile.FullName);
            return true;

        }

        public static bool SaveObject(string fileName, object saveObject, bool overwrite = false)
        {
            string json = JsonUtility.ToJson(saveObject);
            
            return Save(fileName, json, overwrite);
        }

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

        public static List<string> GetAllObjectsNameInFolder(string path)
        {
            DirectoryInfo directoryInfo = new(SaveFolder + path);

            List<string> allFileNames = new List<string>();

            // Get all save files
            FileInfo[] saveFiles = directoryInfo.GetFiles("*." + SaveExtension);

            foreach (FileInfo fileInfo in saveFiles)
            {
                allFileNames.Add(fileInfo.Name);
            }

            return allFileNames;
        }
    }
}
