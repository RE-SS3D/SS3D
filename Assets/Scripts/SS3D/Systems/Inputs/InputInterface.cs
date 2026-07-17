using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Single authority for "is the pointer currently over an interface". Spans both input UI
    /// stacks: legacy uGUI (via the <see cref="EventSystem"/>) and UI Toolkit runtime panels
    /// (via panel picking). World-click gameplay gates must query this instead of talking to a
    /// single stack, otherwise clicks leak through UI Toolkit overlays (radial menu, machine
    /// interfaces) that the uGUI raycaster does not know about.
    /// </summary>
    public static class InputInterface
    {
        private static readonly List<UIDocument> Documents = new();

        /// <summary>
        /// Registers a runtime UI Toolkit document so its panel participates in pointer queries.
        /// Safe to call multiple times; disabled documents are ignored while querying.
        /// </summary>
        public static void RegisterDocument(UIDocument document)
        {
            if (document == null || Documents.Contains(document))
            {
                return;
            }

            Documents.Add(document);
        }

        /// <summary>
        /// Unregisters a previously registered document. Call from the owner's teardown.
        /// </summary>
        public static void UnregisterDocument(UIDocument document)
        {
            if (document == null)
            {
                return;
            }

            Documents.Remove(document);
        }

        /// <summary>
        /// True when the pointer is over any uGUI element or any registered, enabled UI Toolkit panel.
        /// </summary>
        public static bool IsPointerOverInterface()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            return IsPointerOverToolkitPanel();
        }

        private static bool IsPointerOverToolkitPanel()
        {
            if (Documents.Count == 0)
            {
                return false;
            }

            Vector2 screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            // UI Toolkit runtime panels use a top-left origin, screen space uses bottom-left.
            Vector2 flipped = new(screenPosition.x, Screen.height - screenPosition.y);

            for (int i = 0; i < Documents.Count; i++)
            {
                UIDocument document = Documents[i];
                if (document == null || !document.isActiveAndEnabled)
                {
                    continue;
                }

                VisualElement root = document.rootVisualElement;
                IPanel panel = root?.panel;
                if (panel == null)
                {
                    continue;
                }

                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, flipped);
                if (panel.Pick(panelPosition) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
