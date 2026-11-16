using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Single authority for "is the pointer currently over an interface". Spans both input UI
    /// stacks: legacy uGUI (via <see cref="GraphicRaycaster"/>) and UI Toolkit runtime panels
    /// (via panel picking). World-click gameplay gates must query this instead of talking to a
    /// single stack, otherwise clicks leak through UI Toolkit overlays (radial menu, machine
    /// interfaces) that the uGUI raycaster does not know about.
    /// Also tracks text-entry capture depth so world interactions/selection clear while a field
    /// holds <see cref="InputTextEntryScope"/> (compose, console fields, etc.).
    /// </summary>
    public static class InputInterface
    {
        private static readonly List<UIDocument> Documents = new();
        private static readonly List<RaycastResult> RaycastScratch = new();
        private static int _textCaptureDepth;

        /// <summary>
        /// True while any <see cref="InputTextEntryScope"/> is active. World click/hover gates
        /// should treat this like pointer-over-UI so typing cannot fire gameplay.
        /// </summary>
        public static bool IsCapturingText => _textCaptureDepth > 0;

        /// <summary>
        /// Registers a runtime UI Toolkit document so its panel participates in pointer queries.
        /// Safe to call multiple times; disabled documents are ignored while querying.
        /// Forces the document root to <see cref="PickingMode.Ignore"/> so a fullscreen shell
        /// cannot steal world picks when its content is hidden or also Ignore.
        /// </summary>
        public static void RegisterDocument(UIDocument document)
        {
            if (document == null)
            {
                return;
            }

            EnsureDocumentRootIgnoresPicks(document);

            if (Documents.Contains(document))
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

        /// <summary>Pushed by <see cref="InputTextEntryScope"/>; nested scopes are reference-counted.</summary>
        public static void PushTextCapture()
        {
            _textCaptureDepth++;
        }

        /// <summary>Matched to <see cref="PushTextCapture"/>; never goes below zero.</summary>
        public static void PopTextCapture()
        {
            if (_textCaptureDepth > 0)
            {
                _textCaptureDepth--;
            }
        }

        /// <summary>
        /// True when the pointer is over any uGUI graphic or any registered, pickable UI Toolkit
        /// element, or when text entry is capturing keyboard (compose / focused fields).
        /// Does <b>not</b> use <see cref="EventSystem.IsPointerOverGameObject()"/> —
        /// that path hits UI Toolkit's panel raycaster for the whole document, including
        /// <see cref="PickingMode.Ignore"/> layout roots, so fullscreen UITK shells (map editor)
        /// looked like invisible UI and canceled world clicks / placement releases.
        /// </summary>
        public static bool IsPointerOverInterface()
        {
            if (IsCapturingText)
                return true;

            if (IsPointerOverToolkitPanel())
                return true;

            return IsPointerOverLegacyGraphic();
        }

        /// <summary>
        /// Converts a bottom-left screen position (<see cref="Mouse"/> / <see cref="Input.mousePosition"/>)
        /// into panel coordinates for <see cref="IPanel.Pick"/>. UI Toolkit uses a top-left screen
        /// origin — callers must flip Y before <see cref="RuntimePanelUtils.ScreenToPanel"/>
        /// (Unity Manual: FAQ for input and event systems with UI Toolkit).
        /// </summary>
        public static Vector2 ScreenToPanel(IPanel panel, Vector2 screenPositionBottomLeft)
        {
            Vector2 topLeftScreen = screenPositionBottomLeft;
            topLeftScreen.y = Screen.height - topLeftScreen.y;
            return RuntimePanelUtils.ScreenToPanel(panel, topLeftScreen);
        }

        private static void EnsureDocumentRootIgnoresPicks(UIDocument document)
        {
            VisualElement root = document.rootVisualElement;
            if (root != null && root.pickingMode != PickingMode.Ignore)
            {
                root.pickingMode = PickingMode.Ignore;
            }
        }

        /// <summary>
        /// Current pointer position in bottom-left screen pixels (Input System mouse, with legacy fallback).
        /// </summary>
        public static Vector2 GetPointerScreenPosition()
        {
            return Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
        }

        private static bool IsPointerOverToolkitPanel()
        {
            if (Documents.Count == 0)
            {
                return false;
            }

            Vector2 screenPosition = GetPointerScreenPosition();

            for (int i = 0; i < Documents.Count; i++)
            {
                UIDocument document = Documents[i];
                if (document == null || !document.isActiveAndEnabled)
                {
                    continue;
                }

                // Root may be created when the document is first enabled after RegisterDocument.
                EnsureDocumentRootIgnoresPicks(document);

                VisualElement root = document.rootVisualElement;
                IPanel panel = root?.panel;
                if (panel == null)
                {
                    continue;
                }

                Vector2 panelPosition = ScreenToPanel(panel, screenPosition);
                VisualElement picked = panel.Pick(panelPosition);
                if (picked == null || picked == root)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// uGUI only. Skips UI Toolkit panel raycasters (handled by <see cref="IsPointerOverToolkitPanel"/>).
        /// </summary>
        private static bool IsPointerOverLegacyGraphic()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            Vector2 screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            RaycastScratch.Clear();
            eventSystem.RaycastAll(
                new PointerEventData(eventSystem) { position = screenPosition },
                RaycastScratch);

            for (int i = 0; i < RaycastScratch.Count; i++)
            {
                if (RaycastScratch[i].module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
