#if UNITY_EDITOR
using System.Linq;
using SS3D.Systems.Inventory.Interactions;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// (Re)wires <c>Human.prefab</c>'s <see cref="Hands.PlayerHands"/> list from its nested
    /// <c>HumanHandLeft</c>/<c>HumanHandRight</c> <see cref="Hand"/> components via
    /// <see cref="PrefabUtility"/>, instead of hand-dragging `fileID`s in the Inspector. Concrete first
    /// recipe for the "Human hands wiring remains prefab composition debt" note in this map and
    /// Documents/architecture/systems/entities-prefab-recipes.md § Extension points — the wiring is currently correct, this tool is the
    /// safety net for reproducing/verifying it, not a bug fix.
    /// Also ensures <see cref="HandSearchExtension"/> on the hand body-part prefabs.
    /// </summary>
    /// <remarks>
    /// Order matters: <c>Hands.OnStartServer</c> sets the initially-selected hand to
    /// <c>PlayerHands.FirstOrDefault()</c>, so this preserves the existing Left-then-Right convention
    /// rather than deriving order from <see cref="HandSide"/>'s enum ordinal (`Right = 0`), which would
    /// silently flip the default hand at spawn.
    /// </remarks>
    public static class HandsPrefabSetup
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string HandLeftPrefabPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanHandLeft.prefab";
        private const string HandRightPrefabPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanHandRight.prefab";

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.HandsPrefabSetup.WireBatch</c>
        /// Prefer <see cref="SS3D.Systems.Entities.Editor.HumanPrefabRecipes.RunAllBatch"/> for Human re-runs.</summary>
        public static void WireBatch()
        {
            bool changed = Wire();
            Debug.Log($"[HandsPrefabSetup] {(changed ? "Rewired" : "Already correct — no change to")} Human.prefab hands / Search extension.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static bool Wire()
        {
            bool changed = WirePlayerHandsList();
            changed |= EnsureHandSearchExtension(HandLeftPrefabPath);
            changed |= EnsureHandSearchExtension(HandRightPrefabPath);
            if (changed)
            {
                AssetDatabase.SaveAssets();
            }

            return changed;
        }

        private static bool WirePlayerHandsList()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HumanPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[HandsPrefabSetup] Missing prefab: {HumanPrefabPath}");
                return false;
            }

            try
            {
                if (!prefabRoot.TryGetComponent(out Hands hands))
                {
                    Debug.LogError($"[HandsPrefabSetup] No Hands component on {HumanPrefabPath}");
                    return false;
                }

                Hand[] allHands = prefabRoot.GetComponentsInChildren<Hand>(true);
                Hand left = allHands.FirstOrDefault(h => h.Side == HandSide.Left);
                Hand right = allHands.FirstOrDefault(h => h.Side == HandSide.Right);

                if (left == null || right == null)
                {
                    Debug.LogError(
                        $"[HandsPrefabSetup] Could not find both a Left and Right Hand under {HumanPrefabPath} " +
                        $"(found {allHands.Length} Hand component(s)).");
                    return false;
                }

                Hand[] desired = { left, right };
                if (hands.PlayerHands != null
                    && hands.PlayerHands.Count == desired.Length
                    && hands.PlayerHands.SequenceEqual(desired))
                {
                    return false;
                }

                SerializedObject handsSo = new(hands);
                SerializedProperty listProp = handsSo.FindProperty("PlayerHands");
                listProp.arraySize = desired.Length;
                for (int i = 0; i < desired.Length; i++)
                {
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = desired[i];
                }

                handsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hands);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, HumanPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool EnsureHandSearchExtension(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[HandsPrefabSetup] Missing {path}");
                return false;
            }

            try
            {
                if (root.TryGetComponent(out HandSearchExtension _))
                {
                    return false;
                }

                root.AddComponent<HandSearchExtension>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[HandsPrefabSetup] HandSearchExtension on {path}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
