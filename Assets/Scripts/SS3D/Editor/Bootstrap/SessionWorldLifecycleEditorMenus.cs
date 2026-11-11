using FishNet.Object;
using SS3D.Networking;
using SS3D.Permissions;
using SS3D.Systems.Substances;
using SS3D.Systems.Area;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Audio;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using SS3D.Systems.Examine;
using SS3D.Systems.Electricity;
using SS3D.Systems.Furniture.Disposal;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Persistence;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Roles;
using SS3D.Systems.BugReports;
using SS3D.Systems.Rounds;
using SS3D.Systems.Screens;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using SS3D.UI.MachineInterface;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SS3D.Editor.Bootstrap
{
    /// <summary>
    /// Phase 3h: rebuild NetworkSystemsHub prefab (copying scene SerializeFields), strip Boot/Game SubSystems.
    /// </summary>
    public static class SessionWorldLifecycleEditorMenus
    {
        private const string HubResourcesPath = "Assets/Resources/NetworkSystemsHub.prefab";
        private const string BootScenePath = "Assets/Content/Scenes/Boot.unity";
        private const string GameScenePath = "Assets/Content/Scenes/Game.unity";

        private static readonly string[] BootPersistentSystemNames =
        {
            "SceneSystem",
            "InputSystem",
            "ApplicationInitializerSystem",
            "SessionNetworkSystem",
            "CommandLineArgsSystem",
        };

        private static readonly string[] GameSystemNamesToStrip =
        {
            "RoundSystem",
            "AudioSystem",
            "ItemSystem",
            "MachineUISystem",
            "PermissionSystem",
            "ElectricitySystem",
            "ChatSystem",
            "RoundPlayerSystem",
            "SubstanceSystem",
            "PlayerControlSystem",
            "EntitySystem",
            "MindSystem",
            "AtmosSystem",
            "ExamineSystem",
            "SelectionSystem",
            "CameraSystem",
            "CommsSystem",
            "DisposalSystem",
            "RoleSystem",
            "GamemodeSystem",
            "TileSystem",
            "AreaSystem",
            "IdAccessSystem",
            "PersistenceSystem",
            "LocalSpeechBubblesSystem",
        };

        /// <summary>Types migrated onto the hub, in Awake/register-friendly order.</summary>
        private static readonly Type[] HubComponentTypes =
        {
            typeof(TileResourceLoader),
            typeof(TileSubSystem),
            typeof(PersistenceSubSystem),
            typeof(AreaSubSystem),
            typeof(AreaDebugGizmoDrawer),
            typeof(ElectricitySubSystem),
            typeof(ElectricityDebugGizmoDrawer),
            typeof(AtmosSubSystem),
            typeof(DisposalSubSystem),
            typeof(ServerSettingsService),
            typeof(RoundSubSystem),
            typeof(MapEditorStopFlow),
            typeof(BugReportIntake),
            typeof(ReadyPlayersSubSystem),
            typeof(PlayerSubSystem),
            typeof(EntitySubSystem),
            typeof(MindSubSystem),
            typeof(RoleSubSystem),
            typeof(GamemodeSubSystem),
            typeof(ItemSubSystem),
            typeof(CommsSubSystem),
            typeof(LocalSpeechBubbleController),
            typeof(CommsFeedController),
            typeof(ExamineSubSystem),
            typeof(SelectionSubSystem),
            typeof(CameraSubSystem),
            typeof(AudioSubSystem),
            typeof(PermissionSubSystem),
            typeof(IdAccessSubSystem),
            typeof(SubstancesSubSystem),
            typeof(MachineInterfaceSubSystem),
            typeof(MachineInterfaceHost),
        };

        /// <summary>Batch entry for headless Unity: rebuild hub + strip Boot + strip Game (tier C — no MenuItem).</summary>
        public static void RunPhase3hMigration()
        {
            RebuildNetworkSystemsHubPrefab();
            StripBootPersistentSystems();
            StripGameSystems();
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 3h migration complete (hub rebuild + Boot/Game strip).");
        }

        [MenuItem("SS3D/Bootstrap/Rebuild NetworkSystemsHub Prefab")]
        public static void RebuildNetworkSystemsHubPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Open Game so we can CopySerialized from live scene components.
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            GameObject root = new("NetworkSystemsHub");
            try
            {
                NetworkObject nob = root.AddComponent<NetworkObject>();
                SerializedObject nobSo = new(nob);
                SerializedProperty isGlobal = nobSo.FindProperty("_isGlobal");
                if (isGlobal != null)
                {
                    isGlobal.boolValue = true;
                    nobSo.ApplyModifiedPropertiesWithoutUndo();
                }

                root.AddComponent<NetworkSystemsHub>();

                // UIDocument required by MachineInterfaceHost before that component is added.
                if (root.GetComponent<UIDocument>() == null)
                {
                    root.AddComponent<UIDocument>();
                }

                foreach (Type type in HubComponentTypes)
                {
                    CopyOrAdd(root, type);
                }

                MachineInterfaceHost host = root.GetComponent<MachineInterfaceHost>();
                UIDocument doc = root.GetComponent<UIDocument>();
                if (host != null && doc != null)
                {
                    SerializedObject hostSo = new(host);
                    SerializedProperty docProp = hostSo.FindProperty("_document");
                    if (docProp != null)
                    {
                        docProp.objectReferenceValue = doc;
                        hostSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                RebuildNetworkBehaviours(nob);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HubResourcesPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"Rebuilt {HubResourcesPath} with hub domain SubSystems (SerializeFields copied from Game where present).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static void StripBootPersistentSystems()
        {
            Scene scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            int removed = StripNamedGameObjects(scene, BootPersistentSystemNames);
            if (removed > 0)
            {
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"Boot strip: removed {removed} Persistent Systems GameObject(s).");
        }

        public static void StripGameSystems()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            int removed = StripNamedGameObjects(scene, GameSystemNamesToStrip);
            if (removed > 0)
            {
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"Game strip: removed {removed} Systems GameObject(s). EventSystem left in place.");
        }

        private static void CopyOrAdd(GameObject root, Type type)
        {
            // Resolve the scene source BEFORE adding to root — FindObjectsByType would otherwise
            // often return the just-added hub component and CopySerialized would no-op.
            Component source = null;
            foreach (UnityEngine.Object obj in UnityEngine.Object.FindObjectsByType(
                         type, FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obj is Component candidate && candidate.gameObject != root)
                {
                    source = candidate;
                    break;
                }
            }

            Component dest = root.GetComponent(type);
            if (dest == null)
            {
                dest = root.AddComponent(type);
            }

            if (source != null)
            {
                EditorUtility.CopySerialized(source, dest);
            }
        }

        private static int StripNamedGameObjects(Scene scene, string[] names)
        {
            HashSet<string> set = new(names);
            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                removed += StripRecursive(root, set);
            }

            return removed;
        }

        private static int StripRecursive(GameObject go, HashSet<string> names)
        {
            int removed = 0;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                removed += StripRecursive(go.transform.GetChild(i).gameObject, names);
            }

            if (names.Contains(go.name))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return removed + 1;
            }

            return removed;
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
                behaviourSo.FindProperty("_addedNetworkObject").objectReferenceValue = networkObject;
                behaviourSo.FindProperty("_networkObjectCache").objectReferenceValue = networkObject;
                behaviourSo.FindProperty("_componentIndexCache").intValue = i;
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
