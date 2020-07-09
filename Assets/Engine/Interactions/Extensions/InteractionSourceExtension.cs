using SS3D.Engine.Inventory.Extensions;
using UnityEngine;

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

        public static T GetComponent<T>(this IInteractionSource source) where T : class
        {
            GameObject gameObject = (source as IGameObjectProvider)?.GameObject;
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }
        
        public static T GetComponentInTree<T>(this IInteractionSource source) where T: class
        {
            return GetComponentInTree<T>(source, out IGameObjectProvider _);
        }
        
        public static T GetComponentInTree<T>(this IInteractionSource source, out IGameObjectProvider provider) where T: class
        {
            IInteractionSource current = source;
            while (current != null)
            {
                if (current is IGameObjectProvider gameObjectProvider)
                {
                    T component = gameObjectProvider.GameObject.GetComponent<T>();
                    if (component != null)
                    {
                        provider = gameObjectProvider;
                        return component;
                    }
                }
                current = current.Parent;
            }

            provider = null;
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