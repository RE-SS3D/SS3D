namespace SS3D.Core
{
    public class NetworkedSystem : NetworkedSpessBehaviour
    {
        protected override void OnStart()
        {
            base.OnStart();

            GameSystems.Register(this);
        }
    }
}