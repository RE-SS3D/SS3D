using SS3D.Interactions.Interfaces;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    [SuppressMessage("Type Safety", "UNT0014:Invalid type for call to GetComponent", Justification = "Temporary")]
    public static class InteractionSourceExtension
    {
        public static IInteractionSource GetRootSource(this IInteractionSource source)
        {
            IInteractionSource current = source;
            while (current.Source != null)
            {
                current = current.Source;
            }

            return current;
        }

        public static T GetComponent<T>(this IInteractionSource source)
            where T : class
        {
            GameObject gameObject = (source as IGameObjectProvider)?.GameObject;
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

		public static T GetComponentInParent<T>(this IInteractionSource source)
            where T : class
		{
			GameObject gameObject = (source as IGameObjectProvider)?.GameObject;
            return gameObject != null ? gameObject.GetComponentInParent<T>() : null;
		}

		public static T GetComponentInTree<T>(this IInteractionSource source)
            where T : class
        {
            return GetComponentInTree<T>(source, out IGameObjectProvider _);
        }

        public static T GetComponentInTree<T>(this IInteractionSource source, out IGameObjectProvider provider)
            where T : class
        {
            IInteractionSource current = source;
            while (current != null)
            {
                if (current is IGameObjectProvider gameObjectProvider)
                {
                    if (gameObjectProvider.GameObject.TryGetComponent<T>(out T component))
                    {
                        provider = gameObjectProvider;
                        return component;
                    }
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

        // public static Hands GetHands(this IInteractionSource source)
        // {
        //     return source.GetComponentInTree<Hands>();
        // }
        //
        // public static Entity GetEntity(this IInteractionSource source)
        // {
        //     return source.GetRootSource().GetComponent<Entity>();
        // }
    }
}