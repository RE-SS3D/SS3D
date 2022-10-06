namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on networked objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class NetworkedSystem : NetworkedSpessBehaviour
    {
        protected override void OnStart()
        {
            base.OnStart();

            GameSystems.Register(this);
        }
    }
}