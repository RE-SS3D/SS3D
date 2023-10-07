

#if UNITY_EDITOR

using EditorTools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// TODO : Add a list of excluded assemblies from the SS3D folder.
public class RuleSetManager : EditorWindow
{
    private static readonly string OriginalRulesetPath = "Assets/Scripts/SS3D/Analysers/CustomRuleSetInitializer/SS3DRules.ruleset";

    // Define the root folder where you want to copy the ruleset files
    private static string TargetRootFolder = "Assets\\Scripts\\SS3D";

    private static string RulesetName = "SS3DRules.ruleset";

    private static string DisplayMessage = "Find the source ruleset at " + OriginalRulesetPath;

    [MenuItem("Tools/SS3D/RuleSetManager")]
    public static void ShowWindow()
    {
        GetWindow<RuleSetManager>("Ruleset manager");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        Label label = new Label(DisplayMessage);
        root.Add(label);

        // Create button
        Button buttonAdd = new Button();
        buttonAdd.name = "Add ruleset to all SS3D assemblies";
        buttonAdd.text = "Add ruleset to all SS3D assemblies";
        root.Add(buttonAdd);

        Button buttonRemove = new Button();
        buttonRemove.name = "Remove ruleset from all SS3D assemblies";
        buttonRemove.text = "Remove ruleset from all SS3D assemblies";
        root.Add(buttonRemove);

        buttonAdd.clicked += ButtonAddClicked;
        buttonRemove.clicked += buttonRemoveClicked;
    }

    private void ButtonAddClicked()
    {
        // Check if the XML file exists
        if (!File.Exists(OriginalRulesetPath))
        {
            Debug.LogWarning("The source ruleset does not exist.");
            return;
        }

        // Get all .asmdef files in the project
        string[] asmdefFiles = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories);

        foreach (string asmdefFile in asmdefFiles)
        {
            // Get the directory containing the .asmdef file
            string asmdefDirectory = Path.GetDirectoryName(asmdefFile);

            // Check if the directory exists and is under the target root folder
            if (asmdefDirectory.StartsWith(TargetRootFolder))
            {
                // Create the target path for the XML file in the assembly folder
                string targetXMLPath = Path.Combine(asmdefDirectory, Path.GetFileName(OriginalRulesetPath));

                // Copy the XML file to the assembly folder
                File.Copy(OriginalRulesetPath, targetXMLPath, true);

                Debug.Log("Copied ruleset file to: " + targetXMLPath);
            }
        }

        AssetDatabase.Refresh();
    }

    private void buttonRemoveClicked()
    {
        // Get all .asmdef files in the project
        string[] asmdefFiles = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories);

        foreach (string asmdefFile in asmdefFiles)
        {
            // Get the directory containing the .asmdef file
            string asmdefDirectory = Path.GetDirectoryName(asmdefFile);

            // Check if the directory exists and is under the target root folder
            if (asmdefDirectory.StartsWith(TargetRootFolder))
            {
                // Create the target path for the XML file in the assembly folder
                string targetXMLPath = Path.Combine(asmdefDirectory, Path.GetFileName(RulesetName));

                // Copy the XML file to the assembly folder
                File.Delete(targetXMLPath);

                Debug.Log("Removed XML file to: " + targetXMLPath);
            }
        }

        AssetDatabase.Refresh();
    }
}

#endif
