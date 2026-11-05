#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Creates blast VFX catalog, fireball prefab, and floor scorch decal assets.
    /// </summary>
    public static class BlastVfxSetup
    {
        private const string ContentRoot = "Assets/Content/WorldObjects/World/VFX/Structural";
        private const string CatalogPath = "Assets/Resources/BlastVfxCatalog.asset";
        private const string BurstPrefabPath = ContentRoot + "/BlastExplosion.prefab";
        private const string ScorchPrefabPath = ContentRoot + "/ScorchFloorDecal.prefab";
        private const string ScorchMatPath = ContentRoot + "/ScorchDecal.mat";
        private const string ScorchTexPath = ContentRoot + "/ScorchMask.png";
        private const string BloodDecalMatPath = "Assets/Content/WorldObjects/World/VFX/Health/BloodDecal.mat";
        private const string BloodParticleMatPath = "Assets/Content/WorldObjects/World/VFX/Health/BloodParticle.mat";

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.BlastVfxSetup.SetupBatch</c>
        /// Prefer <see cref="StructuralDamageContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            SetupAll();
            Debug.Log("[BlastVfxSetup] Done.");
        }

        public static void SetupAll()
        {
            EnsureFolder(ContentRoot);

            Texture2D scorchTex = EnsureScorchTexture();
            Material scorchMat = EnsureScorchMaterial(scorchTex);
            GameObject scorchPrefab = EnsureScorchPrefab(scorchMat);
            GameObject burstPrefab = EnsureBurstPrefab();
            EnsureCatalog(burstPrefab, scorchPrefab, scorchMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Texture2D EnsureScorchTexture()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(ScorchTexPath);
            if (existing != null)
                return existing;

            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "ScorchMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            const float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - r);
                    alpha = Mathf.Pow(alpha, 1.6f);
                    // Dark brown scorch; URP Decal multiplies Base_Map.
                    Color c = new Color(0.12f, 0.08f, 0.06f, alpha);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(Path.GetFullPath(ScorchTexPath), png);
            AssetDatabase.ImportAsset(ScorchTexPath);

            TextureImporter importer = AssetImporter.GetAtPath(ScorchTexPath) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(ScorchTexPath);
        }

        private static Material EnsureScorchMaterial(Texture2D scorchTex)
        {
            Material blood = AssetDatabase.LoadAssetAtPath<Material>(BloodDecalMatPath);
            Material scorch = AssetDatabase.LoadAssetAtPath<Material>(ScorchMatPath);
            if (scorch == null && blood != null)
            {
                scorch = new Material(blood) { name = "ScorchDecal" };
                AssetDatabase.CreateAsset(scorch, ScorchMatPath);
            }
            else if (scorch == null)
            {
                Debug.LogWarning("[BlastVfxSetup] BloodDecal.mat missing — scorch material not created.");
                return null;
            }

            if (scorchTex != null)
            {
                if (scorch.HasProperty("Base_Map"))
                    scorch.SetTexture("Base_Map", scorchTex);
                if (scorch.HasProperty("_BaseMap"))
                    scorch.SetTexture("_BaseMap", scorchTex);
                if (scorch.HasProperty("_MainTex"))
                    scorch.SetTexture("_MainTex", scorchTex);
            }

            EditorUtility.SetDirty(scorch);
            return scorch;
        }

        private static GameObject EnsureScorchPrefab(Material scorchMat)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ScorchPrefabPath);
            if (existing != null)
                return existing;

            GameObject go = new GameObject("ScorchFloorDecal");
            DecalProjector projector = go.AddComponent<DecalProjector>();
            projector.material = scorchMat;
            projector.scaleMode = DecalScaleMode.ScaleInvariant;
            projector.size = new Vector3(1.2f, 1.2f, 0.18f);
            projector.pivot = new Vector3(0f, 0f, 0.09f);
            projector.drawDistance = 40f;
            projector.startAngleFade = 180f;
            projector.endAngleFade = 180f;
            projector.renderingLayerMask = 1u << 1;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ScorchPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject EnsureBurstPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath);
            if (existing != null)
            {
                if (existing.GetComponent<BlastExplosionEffect>() == null)
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(BurstPrefabPath);
                    if (root.GetComponent<BlastExplosionEffect>() == null)
                        root.AddComponent<BlastExplosionEffect>();
                    if (root.GetComponent<Light>() == null)
                    {
                        Light light = root.AddComponent<Light>();
                        light.type = LightType.Point;
                        light.color = BlastVfxCatalog.FireCoreColor;
                        light.intensity = 8f;
                        light.range = 10f;
                        light.enabled = false;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, BurstPrefabPath);
                    PrefabUtility.UnloadPrefabContents(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath);
            }

            GameObject go = new GameObject("BlastExplosion");
            Light point = go.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = BlastVfxCatalog.FireCoreColor;
            point.intensity = 8f;
            point.range = 10f;
            point.shadows = LightShadows.None;
            point.enabled = false;

            go.AddComponent<BlastExplosionEffect>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BurstPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void EnsureCatalog(GameObject burst, GameObject scorchPrefab, Material scorchMat)
        {
            EnsureFolder("Assets/Resources");

            BlastVfxCatalog catalog = AssetDatabase.LoadAssetAtPath<BlastVfxCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BlastVfxCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject so = new SerializedObject(catalog);
            so.FindProperty("_burstPrefab").objectReferenceValue = burst;
            so.FindProperty("_scorchFloorPrefab").objectReferenceValue = scorchPrefab;
            so.FindProperty("_scorchDecalMaterial").objectReferenceValue = scorchMat;
            so.FindProperty("_particleMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(BloodParticleMatPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
#endif
