#if UNITY_EDITOR

using EditorTools;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// TODO : Add a list of excluded assemblies from the SS3D folder.
public class RuleSetManager : EditorWindow
{
    private const string RulesetRootPath = "Assets/Scripts/SS3D/Analysers/CustomRuleSetInitializer/";

    // Define the root folder where you want to copy the ruleset files
    private const string ScriptsRootFolder = @"Assets\Scripts\SS3D";
    private const string TestsRootFolder = @"Assets\Scripts\Tests";

    private const string RulesetName = "SS3DRules.ruleset";
    private const string ExternalRulesetName = "ExternalRules.ruleset";

    private const string DisplayMessage = "Find the source ruleset at " + RulesetRootPath;

    [NotNull]
    private static string Ss3DRulesetPath => RulesetRootPath + RulesetName;

    [NotNull]
    private static string ExternalRulesetPath => RulesetRootPath + ExternalRulesetName;

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
        buttonRemove.clicked += ButtonRemoveClicked;
    }

    private void ButtonAddClicked()
    {
        // Check if the XML file exists
        if (!File.Exists(RulesetRootPath))
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

            if (asmdefDirectory == null)
            {
                Debug.LogError($"Asmdef directory us null for {asmdefFile}");
                return;
            }

            // Check if the directory exists and is under the target root folder
            if (asmdefDirectory.StartsWith(ScriptsRootFolder, StringComparison.Ordinal) || asmdefDirectory.StartsWith(TestsRootFolder, StringComparison.Ordinal))
            {
                // Create the target path for the XML file in the assembly folder
                string targetXMLPath = Path.Combine(asmdefDirectory, Path.GetFileName(Ss3DRulesetPath));

                // Copy the XML file to the assembly folder
                File.Copy(Ss3DRulesetPath, targetXMLPath, true);

                Debug.Log("Copied ruleset file to: " + targetXMLPath);
            }
            else
            {
                // Create the target path for the XML file in the assembly folder
                string targetXMLPath = Path.Combine(asmdefDirectory, Path.GetFileName(ExternalRulesetPath));

                // Copy the XML file to the assembly folder
                File.Copy(ExternalRulesetPath, targetXMLPath, true);

                Debug.Log("Copied ruleset file to: " + targetXMLPath); 
            }
        }

        AssetDatabase.Refresh();
    }

    private void ButtonRemoveClicked()
    {
        // Get all .asmdef files in the project
        string[] asmdefFiles = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories);

        foreach (string asmdefFile in asmdefFiles)
        {
            // Get the directory containing the .asmdef file
            string asmdefDirectory = Path.GetDirectoryName(asmdefFile);

            // Check if the directory exists and is under the target root folder
            if (asmdefDirectory.StartsWith(ScriptsRootFolder, StringComparison.Ordinal) || asmdefDirectory.StartsWith(TestsRootFolder, StringComparison.Ordinal))
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
