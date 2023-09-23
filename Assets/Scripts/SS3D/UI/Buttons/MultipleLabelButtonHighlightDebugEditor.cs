#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.UI.Buttons
{
    [CustomEditor(typeof(ToggleLabelButton))]
    public class MultipleLabelButtonHighlightDebugEditor : Editor
    {
        private ToggleLabelButton _button;

        public override void OnInspectorGUI()
        {
            bool state = _button.Highlighted;

            string label = state ? "Unhighlight" : "Highlight";

            if (GUILayout.Button(label, GUILayout.Width(120)))
            {
                if (!state)
                {
                    _button.OnPointerEnter(null);
                }
                else
                {
                    _button.OnPointerExit(null);
                }

                EditorUtility.SetDirty(_button);
            }

            base.OnInspectorGUI();
        }

        protected void OnEnable()
        {
            _button = (ToggleLabelButton)target;
        }
    }
}
#endif