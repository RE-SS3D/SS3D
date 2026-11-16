using SS3D.Systems.Entities.Character;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Disables legacy scene/prefab Unity lights that are not owned by a <see cref="LightPower"/> fixture.
    /// Station illumination is fixture-driven only. The isolated character-preview booth owns
    /// its lights separately and must survive station startup.
    /// </summary>
    public static class GameplayLightGuard
    {
        public static void DisableOrphanSceneLights()
        {
#if UNITY_SERVER
            if (!UnityEngine.Application.isEditor)
            {
                return;
            }
#endif

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light == null || light.GetComponentInParent<LightPower>(true) != null
                    || light.GetComponentInParent<CharacterPreviewBooth>(true) != null)
                {
                    continue;
                }

                light.enabled = false;
                light.gameObject.SetActive(false);
            }
        }
    }
}
