namespace SS3D.Content.Systems.Interactions
{
    public interface IIgnitable
    {
        void Ignite();
        void Extinguish();
        bool CanBeLit { get; }
        bool Lit { get; }
    }
}
