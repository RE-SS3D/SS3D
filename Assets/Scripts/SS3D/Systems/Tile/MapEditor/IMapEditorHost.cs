using SS3D.Systems.Tile.GasMarkers;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Context exposed to placement/preview helpers while the map editor is active.
    /// </summary>
    public interface IMapEditorHost
    {
        bool IsActive { get; }
        bool MouseOverUI { get; }
        bool IsDeleting { get; }
        bool IsOrbiting { get; }
        MapEditorTool CurrentTool { get; }

        /// <summary>The gas marker the next placement writes, set in the marker form.</summary>
        GasMarkerRecord GasMarkerDraft { get; }

        /// <summary>Camera used for placement picks while the editor is open.</summary>
        Camera PickCamera { get; }
    }
}
