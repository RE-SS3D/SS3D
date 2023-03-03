namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Base class for a UI that is Unique
    /// </summary>
    public class View : Actor
    {
        internal override void OnEnable()
        {
            base.OnEnable();

            ViewLocator.Register(this);
        }

        internal override void OnDisable()
        {
            base.OnDisable();
            
            ViewLocator.Unregister(this);
        }
    }
}