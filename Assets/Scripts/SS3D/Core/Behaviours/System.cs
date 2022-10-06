namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Used on objects that wont have two of the same time. Should not be instantiated at runtime.
    /// </summary>
    public class System : SpessBehaviour
    {
        protected override void OnStart()
        {
            base.OnStart();

            GameSystems.Register(this);
        }
    }
}