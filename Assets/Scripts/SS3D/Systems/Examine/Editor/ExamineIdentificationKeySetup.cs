using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineIdentificationKeySetup
    {
        /// <summary>Tier C seed — no MenuItem. Call from batch if Examine keys drift.</summary>
        public static void AddIdentificationCardTemplateKeys()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(ExamineCanonicalKeyGenerator.ExamineTableName);
            if (collection == null)
            {
                Debug.LogError($"Could not find string table collection '{ExamineCanonicalKeyGenerator.ExamineTableName}'.");
                return;
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                Debug.LogError("Could not find English table for the Examine collection.");
                return;
            }

            UpsertEntry(englishTable, ExamineIdentificationKeys.OwnerLine, ExamineIdentificationKeys.OwnerFallback);
            UpsertEntry(englishTable, ExamineIdentificationKeys.RoleLine, ExamineIdentificationKeys.RoleFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Damaged, ExamineStructuralIntegrityKeys.DamagedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Cracked, ExamineStructuralIntegrityKeys.CrackedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.TornMetal, ExamineStructuralIntegrityKeys.TornMetalFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.GougedWood, ExamineStructuralIntegrityKeys.GougedWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.SplitWood, ExamineStructuralIntegrityKeys.SplitWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.ChippedGlass, ExamineStructuralIntegrityKeys.ChippedGlassFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.FracturedGlass, ExamineStructuralIntegrityKeys.FracturedGlassFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.DentedDevice, ExamineStructuralIntegrityKeys.DentedDeviceFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.BrokenDevice, ExamineStructuralIntegrityKeys.BrokenDeviceFallback);
            UpsertEntry(englishTable, ExamineSealKeys.Intact, ExamineSealKeys.IntactFallback);
            UpsertEntry(englishTable, ExamineSealKeys.Breached, ExamineSealKeys.BreachedFallback);
            UpsertEntry(englishTable, ExamineTankKeys.Fill, ExamineTankKeys.FillFallback);
            UpsertEntry(englishTable, "items.clothing.glove_insulated.name", "Insulated Gloves");
            UpsertEntry(englishTable, "items.clothing.glove_insulated.desc", "A pair of insulated gloves.");
            UpsertEntry(englishTable, "items.clothing.shoe_hi_tops.name", "High Top Sneakers");
            UpsertEntry(englishTable, "items.clothing.shoe_jackboots.name", "Jackboots");
            UpsertEntry(englishTable, "items.clothing.shoe_jackboots.desc", "A pair of jackboots.");
            UpsertEntry(englishTable, "items.clothing.syndicate_hardsuit.name", "Syndicate Hardsuit");
            UpsertEntry(englishTable, "items.clothing.syndicate_hardsuit.desc", "Armored pressure suit. Sealed with its helmet it keeps out vacuum and cold, but it does not supply air.");
            UpsertEntry(englishTable, "items.clothing.syndicate_hardsuit_helmet.name", "Syndicate Hardsuit Helmet");
            UpsertEntry(englishTable, "items.clothing.syndicate_hardsuit_helmet.desc", "Armored sealed helmet. It completes the hardsuit's seal and doubles as a mask for internals.");
            UpsertEntry(englishTable, "items.clothing.syndicate_magboots.name", "Syndicate Magboots");
            UpsertEntry(englishTable, "items.clothing.syndicate_magboots.desc", "Magnetic boots. Switched on, they hold you to the deck against moving air.");

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log("Added identification card + structural integrity template keys to the Examine English table.");
        }

        /// <summary>
        /// Tier C seed — no MenuItem. Writes the keyword-tagged English text (examine.md §3) for every
        /// key whose fallback constant carries <c>&lt;k=…&gt;</c> markup, plus the substance, low-tank,
        /// item size/stack, short-look and self-presenting keys. The fallback constants are the source; rerun after editing them.
        /// </summary>
        public static void AddExamineFeedbackKeys()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(ExamineCanonicalKeyGenerator.ExamineTableName);
            if (collection == null || collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                Debug.LogError($"Could not find the English table of '{ExamineCanonicalKeyGenerator.ExamineTableName}'.");
                return;
            }

            (string, string)[] entries =
            {
                (ExamineStructuralIntegrityKeys.Damaged, ExamineStructuralIntegrityKeys.DamagedFallback),
                (ExamineStructuralIntegrityKeys.Cracked, ExamineStructuralIntegrityKeys.CrackedFallback),
                (ExamineStructuralIntegrityKeys.TornMetal, ExamineStructuralIntegrityKeys.TornMetalFallback),
                (ExamineStructuralIntegrityKeys.GougedWood, ExamineStructuralIntegrityKeys.GougedWoodFallback),
                (ExamineStructuralIntegrityKeys.SplitWood, ExamineStructuralIntegrityKeys.SplitWoodFallback),
                (ExamineStructuralIntegrityKeys.ChippedGlass, ExamineStructuralIntegrityKeys.ChippedGlassFallback),
                (ExamineStructuralIntegrityKeys.FracturedGlass, ExamineStructuralIntegrityKeys.FracturedGlassFallback),
                (ExamineStructuralIntegrityKeys.FracturedFloor, ExamineStructuralIntegrityKeys.FracturedFloorFallback),
                (ExamineStructuralIntegrityKeys.DamagedPlating, ExamineStructuralIntegrityKeys.DamagedPlatingFallback),
                (ExamineStructuralIntegrityKeys.DentedDevice, ExamineStructuralIntegrityKeys.DentedDeviceFallback),
                (ExamineStructuralIntegrityKeys.BrokenDevice, ExamineStructuralIntegrityKeys.BrokenDeviceFallback),
                (ExamineSealKeys.Intact, ExamineSealKeys.IntactFallback),
                (ExamineSealKeys.Breached, ExamineSealKeys.BreachedFallback),
                (ExamineTankKeys.Fill, ExamineTankKeys.FillFallback),
                (ExamineTankKeys.FillLow, ExamineTankKeys.FillLowFallback),
                (ExamineHealthKeys.DeadSelf, ExamineHealthKeys.DeadSelfFallback),
                (ExamineHealthKeys.DeadOther, ExamineHealthKeys.DeadOtherFallback),
                (ExamineHealthKeys.CardiacArrestSelf, ExamineHealthKeys.CardiacArrestSelfFallback),
                (ExamineHealthKeys.CardiacArrestOther, ExamineHealthKeys.CardiacArrestOtherFallback),
                (ExamineHealthKeys.CriticalSelf, ExamineHealthKeys.CriticalSelfFallback),
                (ExamineHealthKeys.CriticalOther, ExamineHealthKeys.CriticalOtherFallback),
                (ExamineHealthKeys.SelfHurtAndBleeding, ExamineHealthKeys.SelfHurtAndBleedingFallback),
                (ExamineHealthKeys.SelfBleeding, ExamineHealthKeys.SelfBleedingFallback),
                (ExamineHealthKeys.SelfHurt, ExamineHealthKeys.SelfHurtFallback),
                (ExamineHealthKeys.SelfMissing, ExamineHealthKeys.SelfMissingFallback),
                (ExamineHealthKeys.SelfMissingAndHurtAndBleeding, ExamineHealthKeys.SelfMissingAndHurtAndBleedingFallback),
                (ExamineHealthKeys.SelfMissingAndBleeding, ExamineHealthKeys.SelfMissingAndBleedingFallback),
                (ExamineHealthKeys.SelfMissingAndHurt, ExamineHealthKeys.SelfMissingAndHurtFallback),
                (ExamineHealthKeys.OtherHurtAndBleeding, ExamineHealthKeys.OtherHurtAndBleedingFallback),
                (ExamineHealthKeys.OtherBleeding, ExamineHealthKeys.OtherBleedingFallback),
                (ExamineHealthKeys.OtherHurt, ExamineHealthKeys.OtherHurtFallback),
                (ExamineHealthKeys.OtherMissing, ExamineHealthKeys.OtherMissingFallback),
                (ExamineHealthKeys.OtherMissingAndHurtAndBleeding, ExamineHealthKeys.OtherMissingAndHurtAndBleedingFallback),
                (ExamineHealthKeys.OtherMissingAndBleeding, ExamineHealthKeys.OtherMissingAndBleedingFallback),
                (ExamineHealthKeys.OtherMissingAndHurt, ExamineHealthKeys.OtherMissingAndHurtFallback),
            };

            foreach ((string key, string value) in entries)
            {
                UpsertEntry(englishTable, key, value);
            }

            foreach ((string key, string value) in ExamineSubstanceKeys.All)
            {
                UpsertEntry(englishTable, key, value);
            }

            UpsertEntry(englishTable, SS3D.Systems.Furniture.Disposal.DisposalTag.ExamineKey, SS3D.Systems.Furniture.Disposal.DisposalTag.ExamineFallback);

            foreach ((string key, string value) in SS3D.Systems.Interactions.TakeFromCharacterWarning.TableEntries)
            {
                UpsertEntry(englishTable, key, value);
            }

            foreach ((string key, string value) in ExamineDeviceKeys.All)
            {
                UpsertEntry(englishTable, key, value);
            }

            foreach ((string key, string value) in ExamineWeaponKeys.All)
            {
                UpsertEntry(englishTable, key, value);
            }

            foreach ((string key, string value) in ExamineItemKeys.All)
            {
                UpsertEntry(englishTable, key, value);
            }

            foreach ((string key, string value) in PresentedIdentity.TableEntries)
            {
                UpsertEntry(englishTable, key, value);
            }

            foreach ((string key, string value) in CharacterExamineDetail.TableEntries)
            {
                UpsertEntry(englishTable, key, value);
            }

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log("Wrote keyword-tagged examine keys to the Examine English table.");
        }

        /// <summary>Tier C seed — no MenuItem. Prefer <see cref="AddIdentificationCardTemplateKeys"/> which also upserts integrity keys.</summary>
        public static void AddStructuralIntegrityTemplateKeys()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(ExamineCanonicalKeyGenerator.ExamineTableName);
            if (collection == null)
            {
                Debug.LogError($"Could not find string table collection '{ExamineCanonicalKeyGenerator.ExamineTableName}'.");
                return;
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                Debug.LogError("Could not find English table for the Examine collection.");
                return;
            }

            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Damaged, ExamineStructuralIntegrityKeys.DamagedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Cracked, ExamineStructuralIntegrityKeys.CrackedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.TornMetal, ExamineStructuralIntegrityKeys.TornMetalFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.GougedWood, ExamineStructuralIntegrityKeys.GougedWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.SplitWood, ExamineStructuralIntegrityKeys.SplitWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.ChippedGlass, ExamineStructuralIntegrityKeys.ChippedGlassFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.FracturedGlass, ExamineStructuralIntegrityKeys.FracturedGlassFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.DentedDevice, ExamineStructuralIntegrityKeys.DentedDeviceFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.BrokenDevice, ExamineStructuralIntegrityKeys.BrokenDeviceFallback);

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log("Added structural integrity template keys to the Examine English table.");
        }

        private static void UpsertEntry(StringTable table, string key, string value)
        {
            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, value);
                return;
            }

            entry.Value = value;
        }
    }
}
