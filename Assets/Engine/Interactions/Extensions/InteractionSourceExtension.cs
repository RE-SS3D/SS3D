namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionSourceExtension
    {
        public static IInteractionSource GetRootSource(this IInteractionSource source)
        {
            IInteractionSource current = source;
            while (current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }
    }
}