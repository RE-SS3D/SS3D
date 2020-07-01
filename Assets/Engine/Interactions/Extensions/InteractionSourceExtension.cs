using SS3D.Engine.Inventory.Extensions;

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
        
        public static T GetComponentInTree<T>(this IInteractionSource source) where T: class
        {
            IInteractionSource current = source;
            while (current != null)
            {
                if (current is IGameObjectProvider provider)
                {
                    T component = provider.GameObject.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
                current = current.Parent;
            }

            return null;
        }

        public static RangeLimit GetRange(this IInteractionSource source)
        {
            IInteractionRangeLimit limit = source.GetComponentInTree<IInteractionRangeLimit>();
            return limit?.GetInteractionRange() ?? RangeLimit.Max;
        }

        public static Hands GetHands(this IInteractionSource source)
        {
            return source.GetComponentInTree<Hands>();
        }
    }
}