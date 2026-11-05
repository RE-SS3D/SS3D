#if UNITY_EDITOR
using SS3D.Systems.Combat.Editor;
using SS3D.Systems.Inventory.Containers.Editor;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Entities.Editor
{
    /// <summary>
    /// Single discoverable entry point for every registered <c>Human.prefab</c> recipe tool — the
    /// "recipe" convention named in Documents/architecture/systems/entities-prefab-recipes.md § Extension points and
    /// 2026-07_editor-tooling-tiers.md tier B: a <see cref="PrefabUtility"/>-based static per concern,
    /// registered here, instead of an agent needing tribal knowledge of which scattered menu items
    /// exist or need re-running after a merge. Add a line here whenever a new recipe targeting
    /// <c>Human.prefab</c> or its body-part prefabs is added.
    /// </summary>
    /// <remarks>
    /// Scoped to recipes that mutate <c>Human.prefab</c>/its nested body-part prefabs specifically.
    /// Recipes for unrelated prefabs (e.g. <c>MeleePrefabSetup</c> on hand tools, <c>StorageContainerPrefabSetup</c>
    /// on backpacks/lockers) live on their own domain aggregators — they are not included here.
    /// </remarks>
    public static class HumanPrefabRecipes
    {
        [MenuItem("SS3D/Entities/Run All Human Prefab Recipes")]
        public static void RunAllMenu()
        {
            int devHacksRemoved = HumanPrefabHygiene.RemoveDevHacks();
            int containerInteractiveStripped = BodyPartContainerInteractiveStrip.StripAll();
            bool handsRewired = HandsPrefabSetup.Wire();
            bool speechEmitterAdded = HumanPrefabHygiene.EnsureLocalSpeechEmitter();
            bool combatNetworkWired = CombatInteractionNetworkPrefabSetup.Wire();
            bool substanceContainerStripped = SS3D.Systems.Substances.Editor.HumanSubstanceContainerStrip.Strip();

            // Recipes that remove a component directly on a nested body-part prefab (e.g. the strip
            // above) don't retroactively refresh Human.prefab's own stripped mirror of that instance —
            // always resync last regardless of what the other recipes did. See HumanPrefabHygiene
            // .ResyncNestedPrefabInstances and entities.md § Pitfalls.
            ExoSuitPrefabSetup.Wire();
            SuitStoragePrefabSetup.Wire();
            HumanPrefabHygiene.ResyncNestedPrefabInstances();

            EditorUtility.DisplayDialog(
                "Human Prefab Recipes",
                $"Removed {devHacksRemoved} dev-only component(s).\n" +
                $"Stripped root ContainerInteractive from {containerInteractiveStripped} prefab(s).\n" +
                $"Hands wiring: {(handsRewired ? "rewired" : "already correct")}.\n" +
                $"LocalSpeechEmitter: {(speechEmitterAdded ? "added" : "already present")}.\n" +
                $"CombatInteractionNetwork: {(combatNetworkWired ? "added/wired" : "already present")}.\n" +
                $"SubstanceContainer strip: {(substanceContainerStripped ? "stripped" : "already clean")}.\n" +
                "Resynced Human.prefab against its body-part prefabs.",
                "OK");
        }

        /// <summary>BatchMode entry: <c>-executeMethod SS3D.Systems.Entities.Editor.HumanPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            int devHacksRemoved = HumanPrefabHygiene.RemoveDevHacks();
            int containerInteractiveStripped = BodyPartContainerInteractiveStrip.StripAll();
            bool handsRewired = HandsPrefabSetup.Wire();
            bool speechEmitterAdded = HumanPrefabHygiene.EnsureLocalSpeechEmitter();
            bool combatNetworkWired = CombatInteractionNetworkPrefabSetup.Wire();
            bool substanceContainerStripped = SS3D.Systems.Substances.Editor.HumanSubstanceContainerStrip.Strip();
            ExoSuitPrefabSetup.Wire();
            SuitStoragePrefabSetup.Wire();
            HumanPrefabHygiene.ResyncNestedPrefabInstances();

            UnityEngine.Debug.Log(
                $"[HumanPrefabRecipes] Removed {devHacksRemoved} dev-only component(s); " +
                $"stripped root ContainerInteractive from {containerInteractiveStripped} prefab(s); " +
                $"hands wiring {(handsRewired ? "rewired" : "already correct")}; " +
                $"LocalSpeechEmitter {(speechEmitterAdded ? "added" : "already present")}; " +
                $"CombatInteractionNetwork {(combatNetworkWired ? "added/wired" : "already present")}; " +
                $"SubstanceContainer strip {(substanceContainerStripped ? "stripped" : "already clean")}; " +
                "resynced Human.prefab against its body-part prefabs.");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif
