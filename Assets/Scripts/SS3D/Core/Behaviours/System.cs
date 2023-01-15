namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class System : Actor
    {
        protected override void OnAwake()
        {
            base.OnAwake();
            SystemLocator.Register(this);
        }
    }
}