using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Dedicated servers have "Dedicated Server Optimizations" enabled, which strips shaders from the
    /// build. Any Renderer/Light/ParticleSystem that stays enabled still gets registered with the render
    /// pipeline and spams "Trying to access a shader but no shaders were included in the build" warnings,
    /// which floods the log. The server never needs to render tile/item objects, so their visual
    /// components are disabled right after instantiation.
    /// </summary>
    public static class ServerVisualsUtility
    {
        public static void DisableRenderingComponents(GameObject instance)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (Light light in instance.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }

            foreach (ParticleSystem particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.gameObject.SetActive(false);
            }
        }
    }
}
