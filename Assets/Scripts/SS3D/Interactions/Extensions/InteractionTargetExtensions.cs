﻿using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions.Extensions
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

        public static T GetComponentInParent<T>(this IInteractionTarget target) where T : class
        {
            GameObject go;
            if(target is IGameObjectProvider provider)
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
                if(component != null)
                {
                    return component;
                }
                go = go.transform.parent.gameObject;
            }

            return null;
        }
    }
}