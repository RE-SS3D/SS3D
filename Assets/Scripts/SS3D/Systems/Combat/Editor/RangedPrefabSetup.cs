#if UNITY_EDITOR
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>
    /// Wires Phase 3 ranged extensions onto gun prefabs (PrefabUtility — not Human.prefab).
    /// </summary>
    public static class RangedPrefabSetup
    {
        private const string M4 = "Assets/Content/WorldObjects/Items/Weapons/M4.prefab";

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Combat.Editor.RangedPrefabSetup.SetupBatch</c>
        /// Prefer <see cref="CombatContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            int updated = SetupAll();
            Debug.Log($"[RangedPrefabSetup] Updated {updated} prefabs.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 1);
            }
        }

        public static int SetupAll()
        {
            int updated = 0;
            if (EnsureWeapon(M4, RangedWeaponProfile.M4))
            {
                updated++;
            }

            updated += EnergyWeaponPrefabSetup.SetupAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static bool EnsureWeapon(string path, RangedWeaponProfile profile)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[RangedPrefabSetup] Missing {path}");
                return false;
            }

            try
            {
                Configure(root, profile);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[RangedPrefabSetup] Ranged weapon on {path}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void Configure(GameObject root, RangedWeaponProfile profile)
        {
            if (!root.TryGetComponent(out RangedWeaponItemExtension extension))
            {
                extension = root.AddComponent<RangedWeaponItemExtension>();
            }

            Transform muzzle = EnsureMuzzle(root);

            SerializedObject so = new(extension);
            so.FindProperty("_profile.VisualStyle").enumValueIndex = (int)profile.VisualStyle;
            so.FindProperty("_profile.BruteDamage").floatValue = profile.BruteDamage;
            so.FindProperty("_profile.BurnDamage").floatValue = profile.BurnDamage;
            so.FindProperty("_profile.StaminaDamage").floatValue = profile.StaminaDamage;
            so.FindProperty("_profile.KnockdownSeconds").floatValue = profile.KnockdownSeconds;
            so.FindProperty("_profile.CanSever").boolValue = profile.CanSever;
            so.FindProperty("_profile.BaseSpreadDegrees").floatValue = profile.BaseSpreadDegrees;
            so.FindProperty("_profile.RecoilClimbDegrees").floatValue = profile.RecoilClimbDegrees;
            so.FindProperty("_profile.RecoilPerShot").floatValue = profile.RecoilPerShot;
            so.FindProperty("_profile.RecoilDecayPerSecond").floatValue = profile.RecoilDecayPerSecond;
            so.FindProperty("_profile.MovementBloomPerSpeed").floatValue = profile.MovementBloomPerSpeed;
            so.FindProperty("_profile.FalloffStartMeters").floatValue = profile.FalloffStartMeters;
            so.FindProperty("_profile.FalloffEndMeters").floatValue = profile.FalloffEndMeters;
            so.FindProperty("_profile.FalloffExtraSpreadDegrees").floatValue = profile.FalloffExtraSpreadDegrees;
            so.FindProperty("_profile.MaxRangeMeters").floatValue = profile.MaxRangeMeters;
            so.FindProperty("_profile.FireCooldownSeconds").floatValue = profile.FireCooldownSeconds;
            so.FindProperty("_profile.MagazineSize").intValue = profile.MagazineSize;
            so.FindProperty("_profile.ReloadSeconds").floatValue = profile.ReloadSeconds;
            so.FindProperty("_profile.StructuralForce").floatValue = profile.StructuralForce;
            so.FindProperty("_profile.StaminaCost").floatValue = profile.StaminaCost;
            so.FindProperty("_profile.ExhaustionSpreadDegrees").floatValue = profile.ExhaustionSpreadDegrees;
            so.FindProperty("_profile.RequiresBothHands").boolValue = profile.RequiresBothHands;
            so.FindProperty("_profile.RequiredHand").enumValueIndex = (int)profile.RequiredHand;
            so.FindProperty("_muzzle").objectReferenceValue = muzzle;
            SerializedProperty magazine = so.FindProperty("_magazine");
            if (magazine.objectReferenceValue == null)
            {
                magazine.objectReferenceValue = FindMagazine(root);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Direct child whose name ends in "Magazine" (e.g. M4Magazine); the reload clip hands it to the left hand.</summary>
        private static Transform FindMagazine(GameObject root)
        {
            foreach (Transform child in root.transform)
            {
                if (child.name.EndsWith("Magazine", System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// Empty child at the barrel tip. Uses mesh AABB longest axis when a MeshFilter is present
        /// so flash origin sits past the muzzle rather than at the grip.
        /// </summary>
        private static Transform EnsureMuzzle(GameObject root)
        {
            Transform existing = root.transform.Find("Muzzle");
            if (existing != null)
            {
                return existing;
            }

            GameObject muzzleGo = new("Muzzle");
            Transform muzzle = muzzleGo.transform;
            muzzle.SetParent(root.transform, false);

            if (root.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
            {
                Bounds bounds = meshFilter.sharedMesh.bounds;
                Vector3 extents = bounds.extents;
                Vector3 axis = Vector3.forward;
                float max = extents.z;
                if (extents.x > max)
                {
                    max = extents.x;
                    axis = Vector3.right;
                }

                if (extents.y > max)
                {
                    max = extents.y;
                    axis = Vector3.up;
                }

                // Prefer the end farther from Attachment (grip) when that child exists.
                Vector3 positiveTip = bounds.center + (axis * max);
                Vector3 negativeTip = bounds.center - (axis * max);
                Transform attachment = root.transform.Find("Attachment");
                if (attachment != null)
                {
                    float posDist = (positiveTip - attachment.localPosition).sqrMagnitude;
                    float negDist = (negativeTip - attachment.localPosition).sqrMagnitude;
                    Vector3 tip = posDist >= negDist ? positiveTip : negativeTip;
                    Vector3 outward = tip - bounds.center;
                    muzzle.localPosition = tip;
                    if (outward.sqrMagnitude > 0.0001f)
                    {
                        muzzle.localRotation = Quaternion.LookRotation(outward.normalized);
                    }
                }
                else
                {
                    muzzle.localPosition = positiveTip;
                    muzzle.localRotation = Quaternion.LookRotation(axis);
                }
            }
            else
            {
                muzzle.localPosition = new Vector3(0f, 0f, 0.45f);
                muzzle.localRotation = Quaternion.identity;
            }

            return muzzle;
        }
    }
}
#endif
