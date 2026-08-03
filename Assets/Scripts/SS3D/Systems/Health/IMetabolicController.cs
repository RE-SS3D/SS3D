namespace SS3D.Systems.Health
{
    public interface IMetabolicController
    {
        /// <summary>
        /// Advance this controller's metabolism by <paramref name="deltaTime"/> seconds. Server-only.
        /// </summary>
        void MetabolicTick(float deltaTime);
    }
}
