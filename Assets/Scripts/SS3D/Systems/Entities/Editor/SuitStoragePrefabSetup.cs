#if UNITY_EDITOR
using System.Linq;
using FishNet.Object;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Containers.Editor;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Entities.Editor
{
    /// <summary>
    /// Suit storage beside the ExoSuit slot, the EVA controller, and pockets sized so only small
    /// tanks fit (Documents/design/armor.md §3). Run after <see cref="ExoSuitPrefabSetup"/>.
    /// </summary>
    public static class SuitStoragePrefabSetup
    {
        private const string TorsoPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanTorso.prefab";

        public static void Wire()
        {
            LimitPockets();

            GameObject root = PrefabUtility.LoadPrefabContents(SyndicateHardsuitSetup.HumanPath);
            try
            {
                AttachedContainer[] containers = root.GetComponentsInChildren<AttachedContainer>(true);
                AttachedContainer suitSlot = containers.First(c => c.Type == ContainerType.ExoSuit);
                AttachedContainer storage = System.Array.Find(containers, c => c.Type == ContainerType.SuitStorage);
                if (storage == null)
                {
                    GameObject slot = Object.Instantiate(suitSlot.gameObject, suitSlot.transform.parent);
                    slot.name = "SuitStorageContainer";
                    foreach (ExoSuitStorageCondition copied in slot.GetComponents<ExoSuitStorageCondition>())
                    {
                        Object.DestroyImmediate(copied);
                    }

                    Object.DestroyImmediate(slot.GetComponent<ClothContainer>());
                    storage = slot.GetComponent<AttachedContainer>();
                    SerializedObject so = new SerializedObject(storage);
                    so.FindProperty("_type").intValue = (int)ContainerType.SuitStorage;
                    so.FindProperty("_startFilter").objectReferenceValue = null;
                    so.FindProperty("_hideItems").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Reference<SuitStorageCondition>(storage, "_suitContainer", suitSlot);
                Reference<ExoSuitStorageCondition>(suitSlot, "_suitStorageContainer", storage);

                if (root.GetComponent<EvaGearController>() == null)
                {
                    root.AddComponent<EvaGearController>();
                }

                HumanPrefabHygiene.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, SyndicateHardsuitSetup.HumanPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Reference<T>(AttachedContainer owner, string field, AttachedContainer other) where T : Component
        {
            if (!owner.TryGetComponent(out T condition))
            {
                condition = owner.gameObject.AddComponent<T>();
            }

            SerializedObject so = new SerializedObject(condition);
            so.FindProperty(field).objectReferenceValue = other;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LimitPockets()
        {
            GameObject torso = PrefabUtility.LoadPrefabContents(TorsoPath);
            try
            {
                AttachedContainer pocket = torso.GetComponentsInChildren<AttachedContainer>(true).First(c => c.Type == ContainerType.Pocket);
                SerializedObject so = new SerializedObject(pocket);
                so.FindProperty("_maxSizeClass").enumValueIndex = (int)SizeClass.Small;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(torso, TorsoPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(torso); }
        }
    }
}
#endif
