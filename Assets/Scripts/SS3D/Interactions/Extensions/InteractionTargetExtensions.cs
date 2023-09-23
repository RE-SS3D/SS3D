using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    public static class InteractionTargetExtensions
    {
        public static T GetComponent<T>(this IInteractionTarget target)
            where T : Component
        {
            if (target is IGameObjectProvider provider)
            {
                return provider.GameObject.GetComponent<T>();
            }

            return null;
        }

        /// <summary>
		/// Get a component T in parent of a IInteraction target.
		/// </summary>
		public static T GetComponentInParent<T>(this IInteractionTarget target)
			where T : Component
		{
			GameObject go;

			if (target is IGameObjectProvider provider)
			{
				go = provider.GameObject;
			}
			else
			{
				return null;
			}

			while (go != null)
			{
				if (go.TryGetComponent<T>(out T component))
				{
                    return component;
				}

				go = go.transform.parent.gameObject;
			}

			return null;
		}
	}
}