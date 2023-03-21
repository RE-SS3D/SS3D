
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
namespace EditorTools
{
    class GetNameOfAllScript : EditorWindow
    {
        private const string filePath = "Assets/Temp/ScriptsNames.txt";
        private const string scriptsPath = "Assets/Scripts/SS3D";
        private static List<string> scriptNames;

        [MenuItem("Tools/SS3D/Get name of all scripts")]
        public static void ShowWindow()
        {
            GetWindow<GetNameOfAllScript>("Get name of all scripts");
        }
        private void OnGUI()
        {
            GUILayout.Label("Generate name of all SS3D scripts,\n write them in file located at " + filePath + ".");
            if (GUILayout.Button("Generate names"))
            {
                scriptNames = new List<string>();
                DirectoryInfo rootDir = new DirectoryInfo(scriptsPath);
                File.Delete(filePath);
                WalkDirectoryTree(rootDir);
                scriptNames.Sort((x, y) => string.Compare(x, y));
                foreach (string name in scriptNames)
                {
                    File.AppendAllText(filePath, name);
                }
                scriptNames.Clear();
            }
        }

        static void WalkDirectoryTree(DirectoryInfo root)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            files = root.GetFiles("*.cs");

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    scriptNames.Add(fi.Name + "\n");
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }
        }
    }

}
#endif
