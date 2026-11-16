namespace SS3D.Systems.Tile.MapEditor.Commands
{
    public interface IMapEditorCommand
    {
        MapEditorCommandDto ToDto();
        void Apply(MapEditorCommandContext ctx);
        void Revert(MapEditorCommandContext ctx);
    }

    /// <summary>
    /// A command that knows whether its last <see cref="IMapEditorCommand.Apply"/> changed the map (tech-debt P2.4).
    /// One that changed nothing reverts to nothing, and <see cref="MapEditorCommandService.Execute"/> doesn't push it
    /// as an undo step. Commands without it always count as changed.
    /// </summary>
    public interface IReportsChange
    {
        bool Changed { get; }
    }
}
