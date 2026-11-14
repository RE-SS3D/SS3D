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
    /// operatives carry the C-20r and a Stechkin, Security a security pistol and a stun baton, the Doctor
    /// gauze in the medkit and a defibrillator. The first operative each round also gets the Syndicate bomb beacon. Clothing stays with ClothingImportSetup. Re-running it is idempotent.
    /// </summary>
    public static class JobKitSetup
    {
        private const string Loadouts = "Assets/Content/Data/Roles/Loadouts/";
        private const string References = "Assets/Content/Data/ObjectAssetReferences/";

        [MenuItem("SS3D/Roles/Apply Job Kits")]
        public static void Apply()
        {
            RoleLoadout operative = Load("OperativeLoadout");
            operative.HandRightAsset = Reference("C20R");
            AddToStorage(operative, ContainerType.Bag, "Stechkin");
            // One bomb per squad: only the first operative to spawn each round carries its beacon.
            LoadoutContents firstBag = operative.FirstHolderContents.FirstOrDefault(c => c.Slot == ContainerType.Bag);
            if (firstBag == null) operative.FirstHolderContents.Add(firstBag = new LoadoutContents { Slot = ContainerType.Bag });
            AddMissing(firstBag.Items, "SyndicateBombBeacon", 1);
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
