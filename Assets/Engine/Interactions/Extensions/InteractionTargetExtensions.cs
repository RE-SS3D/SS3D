namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionTargetExtensions
    {
        public static T GetComponent<T>(this IInteractionTarget target) where T: class
        {
            if (target is IGameObjectProvider provider)
            {
                return provider.GameObject.GetComponent<T>();
            }

            return null;
        }
    }
}