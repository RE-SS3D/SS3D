using System.Collections.Generic;
using Coimbra;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Shows immediate client feedback while waiting for server interaction confirmation.
    /// </summary>
    public static class InteractionOptimisticFeedback
    {
        private static readonly Dictionary<int, LoadingBar> ActiveBars = new();

        public static void TryBeginDelayed(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (interaction is not IDelayedInteraction delayedInteraction)
            {
                return;
            }

            if (interactionEvent.Source.GetRootSource() is not IGameObjectProvider source)
            {
                return;
            }

            float delay = GetDelay(delayedInteraction);
            if (delay < 0.1f)
            {
                return;
            }

            Clear(source.GameObject.transform);
            ShowLoadingBar(source.GameObject.transform, delay);
        }

        public static bool TryAdoptExisting(Transform sourceTransform, out LoadingBar loadingBar)
        {
            int key = sourceTransform.GetInstanceID();
            if (ActiveBars.TryGetValue(key, out loadingBar) && loadingBar != null)
            {
                return true;
            }

            loadingBar = null;
            return false;
        }

        public static void Clear(Transform sourceTransform)
        {
            if (sourceTransform == null)
            {
                return;
            }

            int key = sourceTransform.GetInstanceID();
            if (!ActiveBars.TryGetValue(key, out LoadingBar loadingBar))
            {
                return;
            }

            ActiveBars.Remove(key);

            if (loadingBar != null)
            {
                loadingBar.GameObject.Dispose(true);
            }
        }

        private static void ShowLoadingBar(Transform parent, float delay)
        {
            LoadingBar loadingBarPrefab = Assets.Get<LoadingBar>(AssetDatabases.WorldSpaceUI, WorldSpaceUI.LoadingBar);
            LoadingBar instance = Object.Instantiate(loadingBarPrefab, parent);
            instance.LocalPosition = new Vector3(0f, 0.5f, 0f);
            instance.Duration = delay;
            ActiveBars[parent.GetInstanceID()] = instance;
        }

        private static float GetDelay(IDelayedInteraction delayedInteraction)
        {
            if (delayedInteraction is DelayedInteraction concrete)
            {
                return concrete.ClientDelay;
            }

            return 0f;
        }
    }
}
