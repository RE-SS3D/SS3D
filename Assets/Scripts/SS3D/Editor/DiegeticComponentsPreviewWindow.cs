#if UNITY_EDITOR
using SS3D.UI.MachineInterface.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    public class DiegeticComponentsPreviewWindow : EditorWindow
    {
        private ActionLog _actionLog;

        [MenuItem("SS3D/Machine Interface/Preview Diegetic Components")]
        public static void OpenWindow()
        {
            DiegeticComponentsPreviewWindow window = GetWindow<DiegeticComponentsPreviewWindow>();
            window.titleContent = new GUIContent("Diegetic Components");
            window.minSize = new Vector2(720, 800);
            window.Show();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.09f, 0.09f, 0.1f);
            root.style.alignItems = Align.Center;

            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Content/Systems/UI/MachineInterface/Templates/DiegeticComponentsPreview.uxml");

            if (template == null)
            {
                root.Add(new Label("Missing DiegeticComponentsPreview.uxml"));
                return;
            }

            TemplateContainer panel = template.CloneTree();
            root.Add(panel);

            _actionLog = panel.Q<ActionLog>("action-log");
            _actionLog?.SetEntries(new[]
            {
                "[07:45] Diagnostic scan initiated",
                "[07:44] Connection established — Port J1",
            });

            DiegeticDeviceShell shell = panel.Q<DiegeticDeviceShell>("device-shell");
            if (shell != null)
            {
                shell.CloseClicked += () => Debug.Log("Diegetic shell close requested.");
            }

            SteelButton scanButton = panel.Q<SteelButton>("scan-button");
            if (scanButton != null)
            {
                scanButton.Clicked += () => Debug.Log("Run Diagnostic clicked.");
            }
        }
    }
}
#endif
