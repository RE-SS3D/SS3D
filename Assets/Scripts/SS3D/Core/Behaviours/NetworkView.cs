namespace SS3D.Core.Behaviours
{
    public class NetworkView : NetworkActor
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            ViewLocator.Register(this);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            ViewLocator.Unregister(this);
        }
    }
}