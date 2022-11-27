namespace SS3D.Systems.Gamemodes
{
    public interface IGamemode
    {
        /// <summary>
        /// Initializes the gamemode.
        /// </summary>
        public void FinalizeGamemode() {}

        /// <summary>
        /// Finalizes the gamemode.
        /// </summary>
        public virtual void InitializeGamemode() {}
    }
}