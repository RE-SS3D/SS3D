using FishNet.Connection;

namespace SS3D.Systems.Tile.MapEditor.Authorizers
{
    /// <summary>
    /// Server-side gate for map editor mutations. v1: admin only; Phase 2 adds Builder role in creative mode.
    /// </summary>
    public interface IMapEditorAuthorizer
    {
        bool TryAuthorize(NetworkConnection conn);
    }
}
