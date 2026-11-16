using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.SettingsPanel
{
    /// <summary>Element builders shared by the Settings screen's parts.</summary>
    internal static class SettingsUi
    {
        internal static VisualElement Element(VisualElement parent, string classes)
        {
            VisualElement element = new();
            AddClasses(element, classes);
            parent.Add(element);
            return element;
        }

        internal static Label Text(VisualElement parent, string text, string classes)
        {
            Label label = new(text);
            AddClasses(label, classes);
            parent.Add(label);
            return label;
        }

        internal static Button ActionButton(VisualElement parent, string name, string text, Action clicked)
        {
            Button button = new(clicked) { name = name, text = text };
            button.AddToClassList("settings-panel__button");
            parent.Add(button);
            return button;
        }

        internal static void AddClasses(VisualElement element, string classes)
        {
            foreach (string name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) element.AddToClassList(name);
        }

        /// <summary>A padlock from two boxes: the font has no glyph for one.</summary>
        internal static VisualElement LockIcon()
        {
            VisualElement icon = new() { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("settings-panel__lock");
            Element(icon, "settings-panel__lock-shackle");
            Element(icon, "settings-panel__lock-body");
            return icon;
        }

        /// <summary>UI Toolkit has no dashed border; stroke one, like the main menu's empty states.</summary>
        internal static void DashedBorder(VisualElement element)
        {
            element.generateVisualContent += context =>
            {
                Painter2D painter = context.painter2D;
                painter.lineWidth = 1;
                painter.strokeColor = new Color(0.847f, 0.847f, 0.847f, 0.18f);
                float width = element.layout.width - 1;
                float height = element.layout.height - 1;
                Vector2[] corners = { new(0.5f, 0.5f), new(width, 0.5f), new(width, height), new(0.5f, height) };
                painter.BeginPath();
                for (int edge = 0; edge < 4; edge++)
                {
                    Vector2 start = corners[edge];
                    Vector2 delta = corners[(edge + 1) % 4] - start;
                    float length = delta.magnitude;
                    for (float distance = 0; distance < length; distance += 10)
                    {
                        painter.MoveTo(start + delta.normalized * distance);
                        painter.LineTo(start + delta.normalized * Mathf.Min(distance + 6, length));
                    }
                }
                painter.Stroke();
            };
        }
    }
}
