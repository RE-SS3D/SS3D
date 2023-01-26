namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Base class for a UI that is Unique
    /// </summary>
    public class View : Actor
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