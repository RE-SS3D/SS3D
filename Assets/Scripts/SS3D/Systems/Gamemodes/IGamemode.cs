namespace SS3D.Systems.GameModes
{
    public interface IGamemode
    {
        public void FinalizeGamemode() {}
        public virtual void InitializeGamemode() {}
    }
}