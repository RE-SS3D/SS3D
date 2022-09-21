﻿using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.UI.Buttons
{
    [CustomEditor(typeof(LabelButton))]
    public class LabelButtonHighlightDebugEditor : Editor
    {
        private LabelButton _button;

        private void OnEnable()
        {
            _button = (LabelButton)target;
        }

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

               //_button.SendMessageUpwards("OnValidate", _button, SendMessageOptions.RequireReceiver);
            }

            base.OnInspectorGUI();
        }
    }
}