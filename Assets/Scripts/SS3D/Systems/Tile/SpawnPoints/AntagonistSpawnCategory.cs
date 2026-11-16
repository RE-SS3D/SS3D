namespace SS3D.Systems.Tile.SpawnPoints
{
    /// <summary>
    /// Antagonist categories that may own map spawn markers (creative-mode §8 /
    /// antagonist-content.md). No runtime consumer yet beyond authoring/save.
    /// </summary>
    public enum AntagonistSpawnCategory : byte
    {
        Traitor = 0,
        MalfunctioningAI = 1,
        NuclearOperatives = 2,
    }
}
