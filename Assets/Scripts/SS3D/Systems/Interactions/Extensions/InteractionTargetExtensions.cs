
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    public static class InteractionTargetExtensions
    {

        /// <summary>
        /// Basic method to get a custom interaction point if at least one is defined on the target game object 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool GetInteractionPoint(this IInteractionTarget target, IInteractionSource source, out Vector3 point)
        {
            point = Vector3.zero;

            if (!target.TryGetGameObject(out GameObject gameObject))
            {
                return false;
            }

            if (gameObject.TryGetComponent(out InteractionPointsProvider provider))
            {
                point = provider.GetClosestPointFromSource(source.GameObject.transform.position);

                return true;
            }

            return false;
        }


        public static GameObject GetGameObject(this IInteractionTarget target)
        {
            if (target is IGameObjectProvider provider)
            {
                return provider.GameObject;
            }
            return null;
        }

        public static bool TryGetGameObject(this IInteractionTarget target, out GameObject gameObject)
        {
            gameObject = null;

            if (target is IGameObjectProvider provider)
            {
                gameObject = provider.GameObject;
                return true;
            }

            return false;
        }

        public static T GetComponent<T>(this IInteractionTarget target) where T : class
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
		public static T GetComponentInParent<T>(this IInteractionTarget target) where T : class
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
				var component = go.gameObject.GetComponent<T>();
				if (component != null)
				{
					return component;
				}
				go = go.transform.parent.gameObject;
			}

			return null;
		}
	}
}
