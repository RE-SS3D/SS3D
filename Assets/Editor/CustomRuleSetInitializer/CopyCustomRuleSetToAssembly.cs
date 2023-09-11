
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CopyCustomRuleSetToAssembly : AssetPostprocessor
{
    // Define the path to your XML file
    private static string sourceXMLPath = "Assets/Editor/CustomRuleSetInitializer/SS3DRules.ruleset";

    // Define the root folder where you want to copy the XML file
    private static string targetRootFolder = "Assets\\Scripts\\SS3D";

    static CopyCustomRuleSetToAssembly()
    {
        // Register the method to be called when Unity opens
        EditorApplication.delayCall += CopyXMLToAssemblyFoldersWithAsmdefCallback;
    }

    static void CopyXMLToAssemblyFoldersWithAsmdefCallback()
    {
        // Check if the XML file exists
        if (!File.Exists(sourceXMLPath))
        {
            Debug.LogWarning("The source XML file does not exist.");
            return;
        }

        // Get all .asmdef files in the project
        string[] asmdefFiles = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories);

        foreach (string asmdefFile in asmdefFiles)
        {
            // Get the directory containing the .asmdef file
            string asmdefDirectory = Path.GetDirectoryName(asmdefFile);

            // Check if the directory exists and is under the target root folder
            if (asmdefDirectory.StartsWith(targetRootFolder))
            {
                // Create the target path for the XML file in the assembly folder
                string targetXMLPath = Path.Combine(asmdefDirectory, Path.GetFileName(sourceXMLPath));

                // Copy the XML file to the assembly folder
                File.Copy(sourceXMLPath, targetXMLPath, true);

                Debug.Log("Copied XML file to: " + targetXMLPath);
            }
        }
    }
}

