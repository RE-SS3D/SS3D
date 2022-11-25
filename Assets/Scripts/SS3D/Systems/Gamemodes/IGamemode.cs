namespace SS3D.Systems.Gamemodes
{
    public interface IGamemode
    {
        public void FinalizeGamemode() {}
        public virtual void InitializeGamemode() {}
    }
}