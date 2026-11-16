using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Hides in-game HUD roots while the map editor is active.
    /// The editor is reparented out of <c>Player Canvas</c> so that canvas can be disabled.
    /// </summary>
    internal sealed class MapEditorGameplayHud
    {
        private static readonly string[] SuppressedRootNames =
        {
            "PlayerCanvas",
            "Player Canvas",
            "IngameDebugConsole",
            "BandwithDisplayDebug",
        };

        private readonly Transform _editorRoot;
        private readonly List<(GameObject gameObject, bool wasActive)> _suppressed = new();

        private Transform _originalParent;
        private int _originalSiblingIndex;

        public MapEditorGameplayHud(Transform editorRoot)
        {
            _editorRoot = editorRoot;
        }

        public void SetVisible(bool visible)
        {
            if (visible)
                Restore();
            else
                Suppress();
        }

        private void Suppress()
        {
            Restore();

            _originalParent = _editorRoot.parent;
            _originalSiblingIndex = _editorRoot.GetSiblingIndex();
            _editorRoot.SetParent(null, false);

            foreach (string name in SuppressedRootNames)
            {
                GameObject go = GameObject.Find(name);
                if (go == null || IsEditorHierarchy(go.transform))
                    continue;

                _suppressed.Add((go, go.activeSelf));
                go.SetActive(false);
            }
        }

        private void Restore()
        {
            foreach ((GameObject gameObject, bool wasActive) in _suppressed)
            {
                if (gameObject != null)
                    gameObject.SetActive(wasActive);
            }

            _suppressed.Clear();

            if (_originalParent != null)
            {
                _editorRoot.SetParent(_originalParent, false);
                _editorRoot.SetSiblingIndex(_originalSiblingIndex);
                _originalParent = null;
            }
        }

        private bool IsEditorHierarchy(Transform candidate) =>
            candidate == _editorRoot || candidate.IsChildOf(_editorRoot);
    }
}
