
using UnityEngine;
using UnityEditor;
using System.IO;


class GetNameOfAllScript : EditorWindow
{
    private const string filePath = "Assets/Temporary/ScriptsNames.txt";
    private const string scriptsPath = "Assets/Scripts/SS3D";
    [MenuItem("Tools/SS3D/Get name of all scripts")]
    public static void ShowWindow()
    {
        GetWindow<GetNameOfAllScript>("Get name of all scripts");
    }
    private void OnGUI()
    {
        GUILayout.Label("Generate name of all SS3D scripts, write them in file.");
        if (GUILayout.Button("Generate names"))
        {
            DirectoryInfo rootDir = new DirectoryInfo(scriptsPath);
            File.Delete(filePath);
            WalkDirectoryTree(rootDir);
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
                File.AppendAllText(filePath, fi.Name + "\n");
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
