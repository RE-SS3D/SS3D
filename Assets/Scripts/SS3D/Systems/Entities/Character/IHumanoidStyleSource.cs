using UnityEngine;

namespace SS3D.Systems.Entities.Character
{
    /// <summary>
    /// Resolves replicated hair/beard style indices to prefabs. The lobby asset catalog registers
    /// itself at UI bootstrap; headless servers have none and render no styles.
    /// </summary>
    public interface IHumanoidStyleSource
    {
        /// <returns>The head-hair prefab, or null for none/unknown.</returns>
        GameObject GetHairStyle(int index);

        /// <returns>The facial-hair prefab, or null for none/unknown.</returns>
        GameObject GetBeardStyle(int index);

        GameObject GetEyebrowStyle(string id);
    }

    public static class HumanoidStyleSource
    {
        public static IHumanoidStyleSource Current { get; set; }
    }
}
