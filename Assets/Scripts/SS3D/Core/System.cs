namespace SS3D.Core
{
    public class System : SpessBehaviour
    {
        protected override void OnStart()
        {
            base.OnStart();

            GameSystems.Register(this);
        }
    }
}