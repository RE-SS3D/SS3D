#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Containers;
using UnityEditor;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Roles.Editor
{
    /// <summary>
    /// Puts the Playtest 1 weapons and medical gear into the job loadouts (mvp1-nuke-ops.md M0/M1c):
    /// operatives carry a Stechkin sidearm and their telecrystal uplink, Security a security pistol and a stun
    /// baton, the Doctor gauze in the medkit and a defibrillator. The uplink replaces the operative's fixed weapons
    /// and explosives (antagonist-content.md §6): the C-20r, C4, minibomb and Syndicate bomb beacon are bought on it.
    /// Clothing stays with ClothingImportSetup. Re-running it is idempotent.
    /// </summary>
    public static class JobKitSetup
    {
        private const string Loadouts = "Assets/Content/Data/Roles/Loadouts/";
        private const string References = "Assets/Content/Data/ObjectAssetReferences/";

        [MenuItem("SS3D/Roles/Apply Job Kits")]
        public static void Apply()
        {
            RoleLoadout operative = Load("OperativeLoadout");
            // The uplink replaces the fixed kit: what it sells is bought, not issued. Operatives keep the survival
            // gear (hardsuit, helmet, tank, magboots, tools) and a sidearm, like the SS14 and /tg/ operative.
            operative.HandRightAsset = null;
            AddToStorage(operative, ContainerType.Bag, "Stechkin");
            // Each operative carries a personal telecrystal uplink (OperativeUplinkPrefabSetup builds it).
            AddToStorage(operative, ContainerType.Bag, "OperativeUplink");
            RemoveEverywhere(operative, "C20R", "C4", "SyndicateMiniBomb", "SyndicateBombBeacon");
            Save(operative);

            RoleLoadout security = Load("SecurityLoadout");
            security.HandRightAsset = Reference("SecurityPistol");
            AddToStorage(security, ContainerType.Bag, "StunBaton");
            Save(security);

            RoleLoadout doctor = Load("DoctorLoadout");
            // The medkit holds six: gauze takes the place of its two medical patches (one stays on the belt).
            ObjectAssetReference patch = Reference("MedicalPatch");
            doctor.HandLeftContents.RemoveAll(r => r == patch);
            AddMissing(doctor.HandLeftContents, "Gauze", 2);
            AddToStorage(doctor, ContainerType.Bag, "PortableDefibrillator");
            Save(doctor);

            DB.SaveAssets();
        }

        /// <summary>Drops every copy of <paramref name="items"/> from the loadout's storage contents.</summary>
        private static void RemoveEverywhere(RoleLoadout loadout, params string[] items)
        {
            List<ObjectAssetReference> references = items.Select(Reference).ToList();
            foreach (LoadoutContents contents in loadout.EquipmentContents.Concat(loadout.FirstHolderContents))
                contents.Items.RemoveAll(references.Contains);
            loadout.HandLeftContents.RemoveAll(references.Contains);
            loadout.HandRightContents.RemoveAll(references.Contains);
            loadout.FirstHolderContents.RemoveAll(c => c.Items.Count == 0);
        }

        private static void AddToStorage(RoleLoadout loadout, ContainerType slot, string item)
        {
            LoadoutContents contents = loadout.EquipmentContents.FirstOrDefault(c => c.Slot == slot);
            if (contents == null)
            {
                contents = new LoadoutContents { Slot = slot };
                loadout.EquipmentContents.Add(contents);
            }

            AddMissing(contents.Items, item, 1);
        }

        /// <summary>Tops <paramref name="items"/> up to <paramref name="count"/> copies of <paramref name="item"/>.</summary>
        private static void AddMissing(List<ObjectAssetReference> items, string item, int count)
        {
            ObjectAssetReference reference = Reference(item);
            for (int i = items.Count(r => r == reference); i < count; i++) items.Add(reference);
        }

        private static RoleLoadout Load(string name)
        {
            RoleLoadout loadout = DB.LoadAssetAtPath<RoleLoadout>(Loadouts + name + ".asset");
            if (loadout == null) throw new System.InvalidOperationException("Missing loadout " + name);
            return loadout;
        }

        private static ObjectAssetReference Reference(string name)
        {
            ObjectAssetReference reference = DB.LoadAssetAtPath<ObjectAssetReference>(References + name + ".asset");
            if (reference == null) throw new System.InvalidOperationException("Missing item reference " + name + "; run its prefab recipe first");
            return reference;
        }

        private static void Save(RoleLoadout loadout) => EditorUtility.SetDirty(loadout);
    }
}
#endif
