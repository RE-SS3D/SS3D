#if UNITY_EDITOR
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Tier-B recipe for D10 of 2026-11_construction-mvp1: <see cref="TileObjectSo.reinforced"/> on the
    /// reinforced wall and window, and a higher integrity for the reinforced wall, which had plain-wall HP.
    /// Registered on <c>ConstructionContentPrefabRecipes</c>. Idempotent.
    /// </summary>
    public static class ConstructionReinforcedRecipe
    {
        public const string WallPath = "Assets/Content/Data/TileMap/Resources/Structures/Walls/SteelWallReinforced.asset";
        public const string WindowPath = "Assets/Content/Data/TileMap/Resources/Structures/Walls/SteelWindowReinforced.asset";

        /// <summary>
        /// First guess for the balancing pass: above a plain wall (100) so melee and stray blasts take
        /// longer, below one attached C4 at its epicentre (180), so the vault stays the operatives' C4 problem.
        /// </summary>
        public const float ReinforcedWallIntegrity = 160f;

        public static readonly (string Path, float MaxIntegrity)[] Targets =
        {
            (WallPath, ReinforcedWallIntegrity),
            (WindowPath, 0f),
        };

        public static int SetupAll()
        {
            int changed = 0;
            foreach ((string path, float maxIntegrity) in Targets)
            {
                TileObjectSo tile = AssetDatabase.LoadAssetAtPath<TileObjectSo>(path);
                if (tile == null)
                {
                    Debug.LogError($"[ConstructionReinforcedRecipe] Missing {path}");
                    continue;
                }

                if (tile.reinforced && (maxIntegrity <= 0f || Mathf.Approximately(tile.structuralMaxIntegrity, maxIntegrity)))
                {
                    continue;
                }

                tile.reinforced = true;
                if (maxIntegrity > 0f)
                {
                    tile.structuralMaxIntegrity = maxIntegrity;
                }

                EditorUtility.SetDirty(tile);
                changed++;
            }

            AssetDatabase.SaveAssets();
            return changed;
        }
    }
}
#endif
