namespace SS3D.Systems.Gamemodes
{
    public interface IGamemode
    {
        /// <summary>
        /// Finalizes the gamemode.
        /// </summary>
        public void FinalizeGamemode() { }

        /// <summary>
        /// Initializes the gamemode.
        /// </summary>
        public virtual void InitializeGamemode() { }
    }
}