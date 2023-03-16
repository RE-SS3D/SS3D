using JetBrains.Annotations;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    public static class InteractionSourceExtension
    {
        [NotNull]
        public static IInteractionSource GetRootSource(this IInteractionSource source)
        {
            IInteractionSource current = source;
            while (current.Source != null)
            {
                current = current.Source;
            }

            return current;
        }

        [CanBeNull]
        public static T GetComponent<T>(this IInteractionSource source) where T : class
        {
            GameObject gameObject = (source as IGameObjectProvider)?.ProvidedGameObject;
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }
        
        [CanBeNull]
        public static T GetComponentInTree<T>(this IInteractionSource source) where T: class
        {
            return GetComponentInTree<T>(source, out IGameObjectProvider _);
        }
        
        [CanBeNull]
        public static T GetComponentInTree<T>(this IInteractionSource source, [CanBeNull] out IGameObjectProvider provider) where T: class
        {
            IInteractionSource current = source;

            // TODO: Remove while, as while can cause unexpected issues such as crashes or freezes during execution if used on loop timing calls.
            while (current != null)
            {
                if (current is IGameObjectProvider gameObjectProvider)
                {
                    GameObject gameObject = gameObjectProvider.ProvidedGameObject;

                    T component = gameObject.GetComponent<T>();
                    if (component == null)
                    {
                        provider = null;
                        return null;
                    }

                    provider = gameObjectProvider;
                    return component;
                }

                current = current.Source;
            }

            provider = null;
            return null;
        }

        public static RangeLimit GetRange(this IInteractionSource source)
        {
            IInteractionRangeLimit limit = source.GetComponentInTree<IInteractionRangeLimit>();
            return limit?.GetInteractionRange() ?? RangeLimit.Max;
        }
    }    
}