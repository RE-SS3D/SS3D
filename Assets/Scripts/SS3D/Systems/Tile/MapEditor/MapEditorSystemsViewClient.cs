using System;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Client half of the systems views (Documents/architecture/2026-11_map-editor-systems-views.md Q2, Q10): which view
    /// it asked the server for, the structure the server has sent for it, and the overlay drawing that structure.
    /// The RPCs live on the hub, in <c>MapEditorSystemsView.cs</c>.
    /// </summary>
    public sealed class MapEditorSystemsViewClient : IDisposable
    {
        private readonly MapEditorSystemsOverlay _overlay = new();

        /// <summary>The view last asked of the server.</summary>
        public SystemsViewKind Requested { get; private set; }

        /// <summary>What the server has sent for <see cref="Requested"/>; null until its reset arrives.</summary>
        public SystemsViewStructure Structure { get; private set; }

        /// <summary>
        /// Every frame while the editor is open, with the view now on. Returns true when that differs from
        /// <see cref="Requested"/>, so the server must be asked for it; the old view's structure is dropped.
        /// </summary>
        public bool Update(SystemsViewKind active)
        {
            bool changed = active != Requested;
            if (changed)
            {
                Requested = active;
                Structure = null;
                _overlay.SetStructure(null);
                _hoverRevision++;
            }

            _overlay.SetVisible(active != SystemsViewKind.Off);
            return changed;
        }

        /// <summary>
        /// A delta from the server. One for a view switched away from may still be in flight, so it is dropped, as is
        /// anything before the new view's reset.
        /// </summary>
        public bool Receive(SystemsViewDelta delta)
        {
            if (delta.View != Requested || delta.View == SystemsViewKind.Off)
                return false;

            if (delta.Reset)
                Structure = new SystemsViewStructure(delta.View);
            else if (Structure == null)
                return false;

            Structure.Apply(delta);
            _overlay.Apply(Structure, delta);
            _hoverRevision++;
            return true;
        }

        private int _hoverRevision;
        private int _describedRevision = -1;
        private Vector2Int _describedTile;
        private string _described = string.Empty;

        /// <summary>
        /// What the view shows on <paramref name="tile"/> in words ("Supply 3", "Pump: on · Surface 2 → Surface 5"), or
        /// empty; worded by the view's domain (<see cref="ISystemsViewStyle.Describe"/>). Cached per tile and delta.
        /// </summary>
        public string HoverText(Vector2Int tile)
        {
            if (Structure == null || Requested == SystemsViewKind.Off)
                return string.Empty;

            if (_describedRevision == _hoverRevision && _describedTile == tile)
                return _described;

            _describedRevision = _hoverRevision;
            _describedTile = tile;
            _described = SystemsViewStyles.For(Structure.View)?.Describe(Structure, tile) ?? string.Empty;
            return _described;
        }

        /// <summary>
        /// The cursor badge with a view on: what is being placed, then what the view shows under the cursor, so a pipe's
        /// line and network show on Lower › Gas Pipes too, where the view turns itself on while you place.
        /// </summary>
        public static string CursorLabel(string placing, string hover) =>
            string.IsNullOrEmpty(hover) ? placing ?? string.Empty
            : string.IsNullOrEmpty(placing) ? hover
            : placing + "\n" + hover;

        /// <summary>The overlay, for tests and the visual pilot.</summary>
        public MapEditorSystemsOverlay Overlay => _overlay;

        /// <summary>The editor closed; the server drops the subscription with the editor-open report.</summary>
        public void Stop()
        {
            Requested = SystemsViewKind.Off;
            Structure = null;
            _overlay.SetStructure(null);
            _overlay.SetVisible(false);
        }

        public void Dispose() => _overlay.Dispose();
    }
}
