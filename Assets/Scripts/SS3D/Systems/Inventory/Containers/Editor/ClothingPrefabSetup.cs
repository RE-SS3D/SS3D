#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Wires <see cref="ClothingItemPresentation"/> + root world collider on jumpsuit base
    /// (variants inherit). PrefabUtility — do not hand-edit mega-prefabs.
    /// </summary>
    public static class ClothingPrefabSetup
    {
        private const string GreyJumpsuit = "Assets/Content/WorldObjects/Items/Clothing/JumpsuitGrey.prefab";
        private const string WornChildName = "Jumpsuit";
        private const string FoldedChildName = "JumpsuitFolded";

        // Sized from the retired JumpsuitSecurityFolded orphan prefab.
        private static readonly Vector3 ColliderSize = new(0.43f, 0.173f, 0.351f);
        private static readonly Vector3 ColliderCenter = new(0f, 0.015f, -0.015f);

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.ClothingPrefabSetup.SetupBatch</c>
        /// Prefer <see cref="InventoryContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            int updated = SetupAll();
            Debug.Log($"[ClothingPrefabSetup] Updated {updated} prefab(s).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 1);
            }
        }

        public static int SetupAll()
        {
            int updated = 0;
            if (SetupGrey())
            {
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static void WireCoverage(Cloth cloth)
        {
            const string humanPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
            const string art = "Assets/Art/Models/Entities/Humanoids/Human/";
            GameObject human = AssetDatabase.LoadAssetAtPath<GameObject>(humanPath);
            SerializedObject so = new(cloth);
            string[] hidden = { "HumanTorso", "HumanArmLeft", "HumanArmRight", "UnderwearTop", "UnderwearBottom" };
            SerializedProperty occluded = so.FindProperty("_occludedBodyRenderers");
            occluded.arraySize = hidden.Length;
            for (int i = 0; i < hidden.Length; i++) occluded.GetArrayElementAtIndex(i).stringValue = hidden[i];
            SerializedProperty overrides = so.FindProperty("_bodyMeshOverrides");
            overrides.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                string name = i == 0 ? "HumanLegLeft" : "HumanLegRight";
                SkinnedMeshRenderer body = human.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == name);
                // The trouser hem is near 0.14 m. Keep a small overlap above it to avoid a bare-ankle gap.
                Mesh source = body.sharedMesh;
                Matrix4x4[] matrices = body.bones.Select((bone, index) =>
                    human.transform.worldToLocalMatrix * bone.localToWorldMatrix * source.bindposes[index]).ToArray();
                BoneWeight[] weights = source.boneWeights;
                float[] heights = source.vertices.Select((v, index) =>
                {
                    BoneWeight w = weights[index];
                    return matrices[w.boneIndex0].MultiplyPoint3x4(v).y * w.weight0
                        + matrices[w.boneIndex1].MultiplyPoint3x4(v).y * w.weight1
                        + matrices[w.boneIndex2].MultiplyPoint3x4(v).y * w.weight2
                        + matrices[w.boneIndex3].MultiplyPoint3x4(v).y * w.weight3;
                }).ToArray();
                string path = art + name + "JumpsuitAnkles.asset";
                Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Mesh generated = ClothingCoverageMeshBuilder.KeepBelow(source, heights, 0.17f, saved);
                if (saved == null) { AssetDatabase.CreateAsset(generated, path); saved = generated; }
                else EditorUtility.SetDirty(saved);
                SerializedProperty entry = overrides.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("RendererName").stringValue = name;
                entry.FindPropertyRelative("BodyMesh").objectReferenceValue = body.sharedMesh;
                entry.FindPropertyRelative("CoveredMesh").objectReferenceValue = saved;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool SetupGrey()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GreyJumpsuit);
            if (root == null)
            {
                Debug.LogError($"[ClothingPrefabSetup] Missing {GreyJumpsuit}");
                return false;
            }

            try
            {
                if (!root.TryGetComponent(out Cloth cloth))
                {
                    Debug.LogError($"[ClothingPrefabSetup] No Cloth on {GreyJumpsuit}");
                    return false;
                }

                WireCoverage(cloth);

                Transform worn = root.transform.Find(WornChildName);
                Transform folded = root.transform.Find(FoldedChildName);
                if (worn == null || folded == null)
                {
                    Debug.LogError(
                        $"[ClothingPrefabSetup] Expected children '{WornChildName}' and '{FoldedChildName}' on {GreyJumpsuit}");
                    return false;
                }

                if (!root.TryGetComponent(out ClothingItemPresentation presentation))
                {
                    presentation = root.AddComponent<ClothingItemPresentation>();
                }

                SerializedObject so = new(presentation);
                so.FindProperty("_worldRoot").objectReferenceValue = folded.gameObject;
                so.FindProperty("_wornShapedRoot").objectReferenceValue = worn.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();

                folded.gameObject.SetActive(true);
                worn.gameObject.SetActive(false);

                if (!root.TryGetComponent(out BoxCollider box))
                {
                    box = root.AddComponent<BoxCollider>();
                }

                box.size = ColliderSize;
                box.center = ColliderCenter;

                PrefabUtility.SaveAsPrefabAsset(root, GreyJumpsuit);
                Debug.Log($"[ClothingPrefabSetup] World presentation on {GreyJumpsuit}");
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
