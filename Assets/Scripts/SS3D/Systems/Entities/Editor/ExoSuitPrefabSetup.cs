#if UNITY_EDITOR
using System.Linq;
using FishNet.Object;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Containers.Editor;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Entities.Editor
{
    /// <summary>Wire the existing ExoSuit equipment type using the ordinary clothing-slot pattern.</summary>
    public static class ExoSuitPrefabSetup
    {
        public static void Wire()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SyndicateHardsuitSetup.HumanPath);
            try
            {
                AttachedContainer[] containers = root.GetComponentsInChildren<AttachedContainer>(true);
                if (!containers.Any(c => c.Type == ContainerType.ExoSuit))
                {
                    AttachedContainer source = containers.First(c => c.Type == ContainerType.Jumpsuit);
                    GameObject slot = Object.Instantiate(source.gameObject, source.transform.parent);
                    slot.name = "ExoSuitContainer";
                    SerializedObject so = new SerializedObject(slot.GetComponent<AttachedContainer>());
                    so.FindProperty("_type").intValue = (int)ContainerType.ExoSuit;
                    so.FindProperty("_startFilter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Filter>("Assets/Content/Data/Filters/Wearables/ExoSuit.asset");
                    so.ApplyModifiedPropertiesWithoutUndo();
                    so = new SerializedObject(slot.GetComponent<ClothContainer>());
                    so.FindProperty("_clothType").intValue = (int)ClothType.ExoSuit;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                AttachedContainer suitSlot = root.GetComponentsInChildren<AttachedContainer>(true).First(c => c.Type == ContainerType.ExoSuit);
                foreach (JumpsuitStorageCondition condition in suitSlot.GetComponents<JumpsuitStorageCondition>())
                {
                    Object.DestroyImmediate(condition);
                }
                if (!root.GetComponentsInChildren<ClothedBodyPart>(true).Any(c => c.Type == ClothType.ExoSuit))
                {
                    SkinnedMeshRenderer overwear = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Overwear");
                    ClothedBodyPart part = overwear.gameObject.AddComponent<ClothedBodyPart>();
                    SerializedObject so = new SerializedObject(part);
                    so.FindProperty("_clothType").intValue = (int)ClothType.ExoSuit;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    overwear.gameObject.SetActive(false);
                }
                ClothedBodyPart suitPart = root.GetComponentsInChildren<ClothedBodyPart>(true).First(c => c.Type == ClothType.ExoSuit);
                // Clothing is addressed by ClothType; a new empty nested NetworkObject cannot resolve reliably.
                if (suitPart.TryGetComponent(out NetworkObject suitNetworkObject)) Object.DestroyImmediate(suitNetworkObject);
                HumanPrefabHygiene.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, SyndicateHardsuitSetup.HumanPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
