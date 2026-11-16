#if UNITY_EDITOR
using FishNet.Object;
using FishNet.Observing;
using SS3D.Interactions;
using SS3D.Systems.Electricity;
using SS3D.Systems.Examine;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Electricity.Editor
{
    /// <summary>
    /// Wires SolarPanel / SolarTrackingBeacon content prefabs (PrefabUtility — tier B).
    /// </summary>
    public static class SolarPrefabSetup
    {
        private const string SolarPanelPath =
            "Assets/Content/WorldObjects/Furniture/Machines/Engineering/SolarPanel.prefab";

        private const string SolarTrackingBeaconPath =
            "Assets/Content/WorldObjects/Furniture/Machines/Engineering/SolarTrackingBeacon.prefab";

        private const string SolarPanelMeshPath =
            "Assets/Art/Models/Furniture/Machines/Engineering/SolarPanel.fbx";

        private const string SolarPanelExaminePath =
            "Assets/Content/Data/Examine/String/Furniture/Machines/Engineering/SolarPanel.asset";

        private const string ObserverConditionPath =
            "Assets/FishNet/Runtime/Observing/Conditions/GridCondition/GridCondition.asset";

        private const string ModelChildName = "Model";

        private static readonly string[] AimVisualCandidateNames =
        {
            // Prefer the FBX root (renamed Model): armature bones often have Blender rest
            // orientations that tumble if yawed as world Euler. Model root yaws about world up.
            ModelChildName,
            "SolarPanelArmature",
            "Armature",
        };

        public static int SetupAll()
        {
            int updated = 0;
            if (SetupSolarPanel())
            {
                updated++;
            }

            if (SetupSolarTrackingBeacon())
            {
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static bool SetupSolarPanel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SolarPanelPath);
            if (root == null)
            {
                Debug.LogError($"[SolarPrefabSetup] Missing {SolarPanelPath}");
                return false;
            }

            try
            {
                bool changed = false;
                changed |= EnsureFullModelAndCollider(root);
                changed |= EnsureComponent<NetworkObject>(root, out NetworkObject networkObject);
                changed |= EnsureComponent<Selectable>(root, out _);
                changed |= EnsureExaminable(root, SolarPanelExaminePath);
                changed |= EnsureNetworkObserver(root, networkObject);
                changed |= EnsureComponent<ElectricDeviceAdjacencyConnector>(root, out _);
                changed |= EnsureComponent<SolarPanel>(root, out SolarPanel panel);

                SerializedObject panelSo = new(panel);
                SerializedProperty peak = panelSo.FindProperty("_peakPowerKw");
                if (peak != null && !Mathf.Approximately(peak.floatValue, 2f))
                {
                    peak.floatValue = 2f;
                    panelSo.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                Transform aimVisual = FindAimVisual(root.transform);
                SerializedProperty aimProp = panelSo.FindProperty("_aimVisual");
                if (aimProp != null && aimVisual != null && aimProp.objectReferenceValue != aimVisual)
                {
                    aimProp.objectReferenceValue = aimVisual;
                    panelSo.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                RebuildNetworkBehaviours(networkObject);

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, SolarPanelPath);
                    Debug.Log($"[SolarPrefabSetup] SolarPanel wired ({SolarPanelPath})");
                }

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool SetupSolarTrackingBeacon()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SolarTrackingBeaconPath);
            if (root == null)
            {
                Debug.LogError($"[SolarPrefabSetup] Missing {SolarTrackingBeaconPath}");
                return false;
            }

            try
            {
                bool changed = false;
                changed |= EnsureComponent<NetworkObject>(root, out NetworkObject networkObject);
                changed |= EnsureComponent<Selectable>(root, out _);
                changed |= EnsureComponent<GenericToggleInteractionTarget>(root, out _);
                changed |= EnsureComponent<SolarTrackingBeacon>(root, out SolarTrackingBeacon beacon);

                SerializedObject beaconSo = new(beacon);
                SerializedProperty radius = beaconSo.FindProperty("_radius");
                if (radius != null && !Mathf.Approximately(radius.floatValue, 8f))
                {
                    radius.floatValue = 8f;
                    beaconSo.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (changed)
                {
                    RebuildNetworkBehaviours(networkObject);
                    PrefabUtility.SaveAsPrefabAsset(root, SolarTrackingBeaconPath);
                    Debug.Log($"[SolarPrefabSetup] SolarTrackingBeacon wired ({SolarTrackingBeaconPath})");
                }

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsureFullModelAndCollider(GameObject root)
        {
            bool changed = false;

            // Strip leftover single-mesh Body from the first recipe pass (panel mesh only).
            Transform legacyBody = root.transform.Find("Body");
            if (legacyBody != null)
            {
                Object.DestroyImmediate(legacyBody.gameObject, true);
                changed = true;
            }

            if (root.TryGetComponent(out SkinnedMeshRenderer rootSkinned))
            {
                Object.DestroyImmediate(rootSkinned, true);
                changed = true;
            }

            if (root.TryGetComponent(out MeshFilter rootFilter))
            {
                Object.DestroyImmediate(rootFilter, true);
                changed = true;
            }

            if (root.TryGetComponent(out MeshRenderer rootRenderer))
            {
                Object.DestroyImmediate(rootRenderer, true);
                changed = true;
            }

            Transform model = root.transform.Find(ModelChildName);
            if (model == null || !HasFullSolarHierarchy(model))
            {
                if (model != null)
                {
                    Object.DestroyImmediate(model.gameObject, true);
                }

                GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(SolarPanelMeshPath);
                if (fbxRoot == null)
                {
                    Debug.LogError($"[SolarPrefabSetup] Missing FBX at {SolarPanelMeshPath}");
                    return changed;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxRoot, root.transform);
                instance.name = ModelChildName;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                model = instance.transform;
                changed = true;
                Debug.Log(
                    $"[SolarPrefabSetup] Instantiated full SolarPanel FBX under '{ModelChildName}' " +
                    $"({CountDescendants(model)} descendants).");
            }

            if (!root.TryGetComponent(out BoxCollider collider))
            {
                collider = root.AddComponent<BoxCollider>();
                changed = true;
            }

            if (TryComputeRendererBounds(root.transform, out Bounds worldBounds))
            {
                Vector3 size = worldBounds.size;
                Vector3 center = root.transform.InverseTransformPoint(worldBounds.center);
                if (collider.size != size || collider.center != center)
                {
                    collider.center = center;
                    collider.size = size;
                    changed = true;
                }
            }

            return changed;
        }

        private static bool HasFullSolarHierarchy(Transform model)
        {
            // Full FBX has base + panel (and usually armature). A lone MeshFilter Body is not enough.
            int meshFilters = model.GetComponentsInChildren<MeshFilter>(true).Length;
            int skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            bool hasArmature = FindDeepChild(model, "SolarPanelArmature") != null
                || FindDeepChild(model, "Armature") != null;
            bool hasBase = FindDeepChild(model, "SolarPanelBase") != null
                || FindDeepChild(model, "Base") != null;
            return (meshFilters + skinned) >= 2 || (hasArmature && hasBase);
        }

        private static Transform FindAimVisual(Transform root)
        {
            Transform model = root.Find(ModelChildName);
            Transform searchRoot = model != null ? model : root;
            for (int i = 0; i < AimVisualCandidateNames.Length; i++)
            {
                Transform found = FindDeepChild(searchRoot, AimVisualCandidateNames[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return model != null ? model : root;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static int CountDescendants(Transform root)
        {
            int count = root.childCount;
            for (int i = 0; i < root.childCount; i++)
            {
                count += CountDescendants(root.GetChild(i));
            }

            return count;
        }

        private static bool TryComputeRendererBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!has)
                {
                    bounds = renderer.bounds;
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return has;
        }

        private static bool EnsureExaminable(GameObject root, string examineAssetPath)
        {
            bool changed = false;
            if (!root.TryGetComponent(out SimpleExaminable examinable))
            {
                examinable = root.AddComponent<SimpleExaminable>();
                changed = true;
            }

            ExamineData data = AssetDatabase.LoadAssetAtPath<ExamineData>(examineAssetPath);
            if (data == null)
            {
                Debug.LogWarning($"[SolarPrefabSetup] Missing examine asset {examineAssetPath}");
                return changed;
            }

            SerializedObject so = new(examinable);
            SerializedProperty key = so.FindProperty("key");
            if (key != null && key.objectReferenceValue != data)
            {
                key.objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            return changed;
        }

        private static bool EnsureNetworkObserver(GameObject root, NetworkObject networkObject)
        {
            bool changed = false;
            if (!root.TryGetComponent(out NetworkObserver observer))
            {
                observer = root.AddComponent<NetworkObserver>();
                changed = true;
            }

            ObserverCondition condition =
                AssetDatabase.LoadAssetAtPath<ObserverCondition>(ObserverConditionPath);
            if (condition != null)
            {
                SerializedObject so = new(observer);
                SerializedProperty conditions = so.FindProperty("_observerConditions");
                if (conditions != null)
                {
                    bool hasCondition = false;
                    for (int i = 0; i < conditions.arraySize; i++)
                    {
                        if (conditions.GetArrayElementAtIndex(i).objectReferenceValue == condition)
                        {
                            hasCondition = true;
                            break;
                        }
                    }

                    if (!hasCondition)
                    {
                        conditions.arraySize = 1;
                        conditions.GetArrayElementAtIndex(0).objectReferenceValue = condition;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }
            }

            SerializedObject nobSo = new(networkObject);
            SerializedProperty observerProp = nobSo.FindProperty("NetworkObserver");
            if (observerProp != null && observerProp.objectReferenceValue != observer)
            {
                observerProp.objectReferenceValue = observer;
                nobSo.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            return changed;
        }

        private static bool EnsureComponent<T>(GameObject root, out T component) where T : Component
        {
            if (root.TryGetComponent(out component))
            {
                return false;
            }

            component = root.AddComponent<T>();
            return true;
        }

        private static void RebuildNetworkBehaviours(NetworkObject networkObject)
        {
            List<NetworkBehaviour> behaviours = new();
            CollectNetworkBehaviours(networkObject.transform, behaviours);

            SerializedObject nobSo = new(networkObject);
            SerializedProperty listProp = nobSo.FindProperty("_networkBehaviours");
            listProp.arraySize = behaviours.Count;
            for (int i = 0; i < behaviours.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = behaviours[i];

                SerializedObject behaviourSo = new(behaviours[i]);
                SerializedProperty added = behaviourSo.FindProperty("_addedNetworkObject");
                SerializedProperty cache = behaviourSo.FindProperty("_networkObjectCache");
                SerializedProperty index = behaviourSo.FindProperty("_componentIndexCache");
                if (added != null)
                {
                    added.objectReferenceValue = networkObject;
                }

                if (cache != null)
                {
                    cache.objectReferenceValue = networkObject;
                }

                if (index != null)
                {
                    index.intValue = i;
                }

                behaviourSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviours[i]);
            }

            nobSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(networkObject);
        }

        private static void CollectNetworkBehaviours(Transform transform, List<NetworkBehaviour> behaviours)
        {
            behaviours.AddRange(transform.GetComponents<NetworkBehaviour>());
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.TryGetComponent(out NetworkObject _))
                {
                    continue;
                }

                CollectNetworkBehaviours(child, behaviours);
            }
        }
    }
}
#endif
