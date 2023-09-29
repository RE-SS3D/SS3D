using System.IO;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class for the saving and loading of serialized objects. Uses Generics and can be adapted for any serializable object.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_EXTENSION = "txt";

        private static readonly string SaveFolder = Application.streamingAssetsPath + "/Saves/";
        private static bool _isInit;

        public static void Init()
        {
            if (!_isInit)
            {
                _isInit = true;
                if (!Directory.Exists(SaveFolder))
                {
                    Directory.CreateDirectory(SaveFolder);
                }
            }
        }

        public static string[] GetSaveFiles()
        {
            Init();
            return Directory.GetFiles(SaveFolder, $"*.{SAVE_EXTENSION}");
        }

        private static void Save(string fileName, string saveString, bool overwrite)
        {
            Init();
            string saveFileName = fileName;
            if (!overwrite)
            {
                // Make sure the Save Number is unique so it doesnt overwrite a previous save file
                int saveNumber = 1;
                while (File.Exists(SaveFolder + saveFileName + "." + SAVE_EXTENSION))
                {
                    saveNumber++;
                    saveFileName = fileName + "_" + saveNumber;
                }
                // saveFileName is unique
            }
            File.WriteAllText(SaveFolder + saveFileName + "." + SAVE_EXTENSION, saveString);
        }

        private static string Load(string fileName)
        {
            Init();
            if (File.Exists(SaveFolder + fileName + "." + SAVE_EXTENSION))
            {
                string saveString = File.ReadAllText(SaveFolder + fileName + "." + SAVE_EXTENSION);
                return saveString;
            }

            return null;
        }

        private static string LoadMostRecentFile()
        {
            Init();
            DirectoryInfo directoryInfo = new(SaveFolder);

            // Get all save files
            FileInfo[] saveFiles = directoryInfo.GetFiles("*." + SAVE_EXTENSION);

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
            if (mostRecentFile != null)
            {
                string saveString = File.ReadAllText(mostRecentFile.FullName);
                return saveString;
            }

            return null;
        }

        public static void SaveObject(object saveObject)
        {
            SaveObject("Save", saveObject, true);
        }

        public static void SaveObject(string fileName, object saveObject, bool overwrite)
        {
            Init();
            string json = JsonUtility.ToJson(saveObject);
            Save(fileName, json, overwrite);
        }

        public static TSaveObject LoadMostRecentObject<TSaveObject>()
        {
            Init();
            string saveString = LoadMostRecentFile();
            if (saveString == null)
            {
                return default;
            }

            TSaveObject saveObject = JsonUtility.FromJson<TSaveObject>(saveString);
            return saveObject;

        }

        public static TSaveObject LoadObject<TSaveObject>(string fileName)
        {
            Init();
            string saveString = Load(fileName);
            if (saveString != null)
            {
                TSaveObject saveObject = JsonUtility.FromJson<TSaveObject>(saveString);
                return saveObject;
            }
            else
            {
                return default;
            }
        }
    }
}
