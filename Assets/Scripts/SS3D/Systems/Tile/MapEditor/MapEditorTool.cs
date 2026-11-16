namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Active map editor toolbar tool. Area is a retained enum slot; room authoring uses
    /// the Rooms category with Edit / Select / Dropper / Delete.
    /// Move is retained for the underlying drag-relocate RPC path even though the current
    /// toolbar UI no longer exposes a dedicated button for it.
    /// </summary>
    public enum MapEditorTool
    {
        Select,
        Edit,
        Move,
        Area,
        Dropper,
        Delete,
    }
}
