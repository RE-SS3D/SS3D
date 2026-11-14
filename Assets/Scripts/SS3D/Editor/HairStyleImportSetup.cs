using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SS3D.UI.Lobby;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Imports hair and facial hair styles from the SS3D-Art repo (lobby effort Phase 5). Each art FBX holds one style
    /// placed on the full-height human, while the game's style prefabs hang off the head bone (see
    /// <c>HumanoidStyleApplier</c>). This copies the FBX in, imports it without materials or animation, and builds a
    /// prefab whose mesh child is moved into head space. The head pivot is read from <c>HumanHair.fbx</c>, whose meshes
    /// are the reference fit. Styles that already have a prefab are skipped. Run it, then rebuild the lobby catalog
    /// and bake the style thumbnails.
    /// </summary>
    public static class HairStyleImportSetup
    {
        public const string DefaultArtRoot = "../SS3D-Art-main/Pulled from Google Drive/SS3D GoogleDrive-20261104T183245Z-1-001/"
            + "SS3D GoogleDrive/Assets/Models/Entities/Humanoids/Human/Hair";

        private const string ModelRoot = "Assets/Art/Models/Entities/Humanoids/Human/Hair/";
        private const string PrefabRoot = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanHair/";
        private const string ReferenceModel = "Assets/Art/Models/Entities/Humanoids/Human/HumanHair.fbx";
        private const string ReferenceMesh = "Hair - Anime";
        private const string HairMaterial = PrefabRoot + "HumanHair.mat";

        /// <summary>The game's style layer (the existing hair prefabs).</summary>
        private const int StyleLayer = 11;

        /// <summary>
        /// Art files that don't fit the head as authored, left out until fixed in Blender: Twintail Extensions is a
        /// 2.3 m strip of loose pieces, Priest floats ~14 cm above the head, both Scholar's styles sit ~17 cm high,
        /// Hipster Ponytail 1 is a bun with no hair on the head, and the Ponytails tails stick forward out of the
        /// forehead (up to 69 cm in front of the head).
        /// </summary>
        public static readonly string[] Skipped =
        {
            "TwintailExtensions", "Priest", "ScholarsLushiousLocks", "ScholarsLushiousBeard", "HipsterPonytail1", "Ponytails",
        };

        private static readonly (string Source, string Prefix, string Prefabs, string Child)[] Kinds =
        {
            ("Head", "HumanHair", "Head", "Hair"),
            ("Facial", "HumanFaceHair", "Face", "Facial Hair"),
        };

        [MenuItem("SS3D/Data/Import Hair Styles From SS3D-Art")]
        public static void ImportMenu() => Debug.Log("[HairImport] " + Import(DefaultArtRoot));

        /// <summary>Imports every style under <paramref name="artRoot"/> (Head/ and Facial/); returns a summary.</summary>
        public static string Import(string artRoot)
        {
            if (!Directory.Exists(artRoot)) return "No art folder at " + Path.GetFullPath(artRoot);
            if (!TryHeadPivot(out Vector3 pivot)) return "No '" + ReferenceMesh + "' in " + ReferenceModel;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(HairMaterial);
            if (material == null) return "No hair material at " + HairMaterial;

            List<(string Model, string Prefix, string Folder, string Child)> pending = new();
            List<string> skipped = new();
            foreach ((string source, string prefix, string prefabs, string child) in Kinds)
            {
                string from = Path.Combine(artRoot, source);
                if (!Directory.Exists(from)) continue;
                Directory.CreateDirectory(ModelRoot + source);
                foreach (string file in Directory.GetFiles(from, "*.fbx").OrderBy(f => f))
                {
                    string name = CleanName(Path.GetFileNameWithoutExtension(file));
                    if (Skipped.Contains(name) || File.Exists(PrefabRoot + prefabs + "/" + prefix + name + ".prefab"))
                    {
                        skipped.Add(prefix + name);
                        continue;
                    }

                    string model = ModelRoot + source + "/" + name + ".fbx";
                    File.Copy(file, model, true);
                    pending.Add((model, prefix, PrefabRoot + prefabs + "/", child));
                }
            }

            AssetDatabase.Refresh();
            int made = 0;
            foreach ((string model, string prefix, string folder, string child) in pending)
            {
                ConfigureImporter(model);
                made += BuildPrefabs(model, prefix, folder, child, pivot, material);
            }

            AssetDatabase.SaveAssets();
            return $"Made {made} style prefabs from {pending.Count} models; skipped {skipped.Count} ({string.Join(", ", skipped)}).";
        }

        /// <summary>"Scholar_s Lushious Locks" → "ScholarsLushiousLocks", "Afro 2" → "Afro2".</summary>
        public static string CleanName(string file)
        {
            StringBuilder name = new();
            foreach (string word in file.Replace("_s ", "s ").Split(' ', '_', '-'))
            {
                string letters = new(word.Where(char.IsLetterOrDigit).ToArray());
                if (letters.Length == 0) continue;
                name.Append(char.ToUpperInvariant(letters[0])).Append(letters.Substring(1));
            }

            return name.ToString();
        }

        /// <summary>The reference style's position in its model: where the head bone sits in art space.</summary>
        private static bool TryHeadPivot(out Vector3 pivot)
        {
            pivot = default;
            GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(ReferenceModel);
            Transform mesh = reference != null ? reference.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == ReferenceMesh) : null;
            if (mesh == null) return false;
            pivot = reference.transform.InverseTransformPoint(mesh.position);
            return true;
        }

        private static void ConfigureImporter(string model)
        {
            if (AssetImporter.GetAtPath(model) is not ModelImporter importer) return;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// One prefab per mesh in the model: a file with one mesh makes "Prefix+FileName"; a file holding several
        /// variants side by side (LongHair, Ponytails: Long / Medium / Short a metre apart) makes "Prefix+MeshName" for
        /// each, moved back to the centre line. The socket the prefab hangs on is the head bone turned -90° about X, so
        /// a mesh at art-space transform T sits at Rx(+90) · Translate(-pivot - sideways offset) · T under the root.
        /// </summary>
        private static int BuildPrefabs(string model, string prefix, string folder, string childName, Vector3 pivot, Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (source == null) return 0;
            List<(Transform Part, Mesh Mesh)> parts = new();
            foreach (Transform part in source.GetComponentsInChildren<Transform>(true))
            {
                Mesh mesh = part.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh
                    : part.TryGetComponent(out SkinnedMeshRenderer skinned) ? skinned.sharedMesh : null;
                if (mesh != null) parts.Add((part, mesh));
            }

            if (parts.Count == 0)
            {
                Debug.LogWarning("[HairImport] No mesh in " + model);
                return 0;
            }

            int made = 0;
            foreach ((Transform part, Mesh mesh) in parts)
            {
                string name = parts.Count == 1 ? Path.GetFileNameWithoutExtension(model) : CleanName(part.name);
                string path = folder + prefix + name + ".prefab";
                if (File.Exists(path)) continue;
                // Variants are laid out on whole metres along X; the worn one sits on the centre line.
                float sideways = Mathf.Round(part.localToWorldMatrix.MultiplyPoint3x4(mesh.bounds.center).x);
                Matrix4x4 local = Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f))
                    * Matrix4x4.Translate(-pivot - new Vector3(sideways, 0f, 0f)) * part.localToWorldMatrix;
                if (SavePrefab(path, childName, mesh, local, material)) made++;
            }

            return made;
        }

        private static bool SavePrefab(string prefabPath, string childName, Mesh mesh, Matrix4x4 local, Material material)
        {
            GameObject root = new(Path.GetFileNameWithoutExtension(prefabPath)) { layer = StyleLayer };
            try
            {
                GameObject child = new(childName) { layer = StyleLayer };
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = local.GetColumn(3);
                child.transform.localRotation = local.rotation;
                child.transform.localScale = local.lossyScale;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, mesh.subMeshCount).ToArray();
                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
