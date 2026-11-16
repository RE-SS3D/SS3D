namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Where a map save is written. v1 always uses LocalTemplate; RoundConfigPool is for creative mode.
    /// </summary>
    public enum MapEditorSaveTarget
    {
        LocalTemplate,
        RoundConfigPool,
    }
}
