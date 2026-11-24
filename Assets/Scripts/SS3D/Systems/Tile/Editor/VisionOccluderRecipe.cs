#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile.Editor
{
    /// <summary>
    /// Tier-B recipe for sight blockers (systems/fov.md Sight blockers). Every wall-type tile that blocks sight gets
    /// a <see cref="VisionOccluder"/>: a centre block and four arms on the <see cref="VisionOccluder.LayerName"/>
    /// layer, as thick as its straight mesh at <see cref="VisionOccluder.EyeHeight"/>. Every leaf of a door that
    /// blocks sight gets a box the size of its mesh, or of its <c>Blocker</c> box when the leaf is a skinned model
    /// (the external airlock); glass doors (<see cref="TileObjectSo.blocksVision"/> off) get none. Removes the old inactive <c>*ViewObstacle</c> children. Idempotent; run from
    /// <b>SS3D → Rendering → Run Content Prefab Recipes</b>. Lives in the Systems assembly so the audit test can
    /// read <see cref="Drift"/>.
    /// </summary>
    public static class VisionOccluderRecipe
    {
        private const string TileRoot = "Assets/Content/Data/TileMap/Resources";
        private const string LegacyObstacleSuffix = "ViewObstacle";
        private const float Tolerance = 0.002f;

        /// <summary>Prefab child whose meshes are the part that blocks sight, when it isn't the root (a plated girder's frame is open).</summary>
        private static readonly Dictionary<string, string> SightMeshChild = new()
        {
            ["SteelGirderPlated"] = "SteelWallSheets",
        };

        private static readonly Direction[] Cardinals = { Direction.North, Direction.East, Direction.South, Direction.West };

        public static int SetupAll()
        {
            int changed = 0;
            foreach (string path in WallPrefabPaths().Blocking)
                changed += Apply(path, SetupWall) ? 1 : 0;
            (List<string> blockingDoors, List<string> glassDoors) = DoorPrefabPaths();
            foreach (string path in blockingDoors)
                changed += Apply(path, SetupDoor) ? 1 : 0;
            foreach (string path in glassDoors)
                changed += Apply(path, RemoveDoorOccluders) ? 1 : 0;

            AssetDatabase.SaveAssets();
            return changed;
        }

        /// <summary>One line per prefab whose occluders differ from what the recipe would build; empty when current.</summary>
        public static List<string> Drift()
        {
            List<string> drift = new();
            (List<string> blocking, List<string> seeThrough) = WallPrefabPaths();
            foreach (string path in blocking)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                drift.AddRange(WallDrift(prefab).Select(problem => $"{path}: {problem}"));
            }

            foreach (string path in seeThrough)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponentInChildren<VisionOccluder>(true) != null || Holders(prefab).Any())
                    drift.Add($"{path}: sees through (window or blocksVision off) but has sight occluders");
            }

            (List<string> blockingDoors, List<string> glassDoors) = DoorPrefabPaths();
            foreach (string path in blockingDoors)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                drift.AddRange(DoorDrift(prefab).Select(problem => $"{path}: {problem}"));
            }

            foreach (string path in glassDoors)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (DoorHolders(prefab).Any())
                    drift.Add($"{path}: sees through (blocksVision off) but has sight occluders");
            }

            return drift;
        }

        /// <summary>Half the wall's thickness across its straight mesh at eye height.</summary>
        public static float HalfThickness(Mesh straight)
        {
            Vector3[] vertices = straight.vertices;
            int[] triangles = straight.triangles;
            float half = 0f;
            float y = VisionOccluder.EyeHeight;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                for (int k = 0; k < 3; k++)
                {
                    Vector3 a = vertices[triangles[t + k]];
                    Vector3 b = vertices[triangles[t + ((k + 1) % 3)]];
                    if ((a.y - y) * (b.y - y) >= 0f)
                        continue;

                    Vector3 cut = Vector3.Lerp(a, b, (y - a.y) / (b.y - a.y));
                    half = Mathf.Max(half, Mathf.Abs(cut.x));
                }
            }

            return half;
        }

        private static (List<string> Blocking, List<string> SeeThrough) WallPrefabPaths()
        {
            List<string> blocking = new();
            List<string> seeThrough = new();
            foreach (TileObjectSo tile in Tiles(TileObjectGenericType.Wall))
            {
                string path = PrefabPath(tile);
                if (path == null)
                    continue;

                bool blocks = tile.blocksVision && !TileOccupancyEvaluator.IsWindowName(tile.NameString);
                (blocks ? blocking : seeThrough).Add(path);
            }

            return (blocking, seeThrough);
        }

        private static (List<string> Blocking, List<string> SeeThrough) DoorPrefabPaths()
        {
            List<string> blocking = new();
            List<string> seeThrough = new();
            foreach (TileObjectSo tile in Tiles(TileObjectGenericType.Door))
            {
                string path = PrefabPath(tile);
                if (path == null || blocking.Contains(path) || seeThrough.Contains(path))
                    continue;

                (tile.blocksVision ? blocking : seeThrough).Add(path);
            }

            return (blocking, seeThrough);
        }

        private static IEnumerable<TileObjectSo> Tiles(TileObjectGenericType type) =>
            AssetDatabase.FindAssets("t:TileObjectSo", new[] { TileRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<TileObjectSo>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(tile => tile != null && tile.genericType == type)
                .OrderBy(tile => tile.name);

        private static string PrefabPath(TileObjectSo tile)
        {
            if (tile.PrefabAsset == null || string.IsNullOrEmpty(tile.PrefabAsset.Id))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(tile.PrefabAsset.Id);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        private static bool Apply(string path, System.Func<GameObject, bool> setup)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!setup(root))
                    return false;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private readonly struct WallShape
        {
            public readonly float Half;
            public readonly float Height;

            public WallShape(float half, float height)
            {
                Half = half;
                Height = height;
            }
        }

        private static bool TryWallShape(GameObject root, out WallShape shape, out string problem)
        {
            shape = default;
            Transform source = SightMeshChild.TryGetValue(root.name, out string child) ? root.transform.Find(child) : root.transform;
            if (source == null)
            {
                problem = $"no sight mesh child {child}";
                return false;
            }

            Mesh straight = StraightMesh(source.gameObject);
            if (straight == null)
            {
                problem = $"{source.name} has no AdvancedConnector straight (i) mesh";
                return false;
            }

            shape = new WallShape(HalfThickness(straight), straight.bounds.max.y);
            problem = null;
            return shape.Half > 0f;
        }

        private static Mesh StraightMesh(GameObject owner)
        {
            foreach (MonoBehaviour behaviour in owner.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null)
                    continue;

                foreach (System.Reflection.FieldInfo field in behaviour.GetType().GetFields(
                             System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    if (field.FieldType == typeof(AdvancedConnector))
                        return ((AdvancedConnector)field.GetValue(behaviour)).i;
                }
            }

            return null;
        }

        private static bool SetupWall(GameObject root)
        {
            bool changed = RemoveLegacyObstacles(root);
            if (!TryWallShape(root, out WallShape shape, out string problem))
            {
                Debug.LogError($"[VisionOccluderRecipe] {root.name}: {problem}");
                return changed;
            }

            if (!WallDrift(root).Any())
                return changed;

            foreach (Transform holder in Holders(root).ToList())
                Object.DestroyImmediate(holder.gameObject);

            GameObject holderObject = NewChild(HolderName, root.transform);
            AddBox(holderObject, "Centre", new Vector3(0f, shape.Height * 0.5f, 0f), new Vector3(shape.Half * 2f, shape.Height, shape.Half * 2f), true);

            GameObject[] arms = new GameObject[4];
            for (int i = 0; i < Cardinals.Length; i++)
            {
                (Vector3 center, Vector3 size) = ArmBox(Cardinals[i], shape);
                arms[i] = AddBox(holderObject, Cardinals[i].ToString(), center, size, false);
            }

            if (!root.TryGetComponent(out VisionOccluder occluder))
                occluder = root.AddComponent<VisionOccluder>();

            SerializedObject so = new(occluder);
            so.FindProperty("_holder").objectReferenceValue = holderObject.transform;
            so.FindProperty("_north").objectReferenceValue = arms[0];
            so.FindProperty("_east").objectReferenceValue = arms[1];
            so.FindProperty("_south").objectReferenceValue = arms[2];
            so.FindProperty("_west").objectReferenceValue = arms[3];
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static IEnumerable<string> WallDrift(GameObject root)
        {
            if (root.GetComponentsInChildren<Transform>(true).Any(t => t.name.EndsWith(LegacyObstacleSuffix)))
                yield return $"still has *{LegacyObstacleSuffix} children";

            if (!TryWallShape(root, out WallShape shape, out string problem))
            {
                yield return problem;
                yield break;
            }

            VisionOccluder occluder = root.GetComponent<VisionOccluder>();
            if (occluder == null || occluder.Holder == null || occluder.Holder.parent != root.transform)
            {
                yield return "no VisionOccluder with a holder on the root";
                yield break;
            }

            if (Holders(root).Count() != 1)
                yield return "more than one occluder holder";

            Transform holder = occluder.Holder;
            if (holder.localPosition != Vector3.zero || holder.gameObject.layer != VisionOccluder.Layer)
                yield return "holder is off the tile centre or off the VisionOccluder layer";

            string centre = BoxDrift(holder.Find("Centre"), new Vector3(0f, shape.Height * 0.5f, 0f),
                new Vector3(shape.Half * 2f, shape.Height, shape.Half * 2f), true);
            if (centre != null)
                yield return $"Centre {centre}";

            foreach (Direction direction in Cardinals)
            {
                GameObject arm = occluder.Arm(direction);
                (Vector3 center, Vector3 size) = ArmBox(direction, shape);
                string armDrift = arm == null || arm.transform.parent != holder
                    ? "missing"
                    : BoxDrift(arm.transform, center, size, false);
                if (armDrift != null)
                    yield return $"{direction} arm {armDrift} (wall mesh is {shape.Half * 2f:F3} m thick at eye height)";
            }
        }

        private static (Vector3 Center, Vector3 Size) ArmBox(Direction direction, WallShape shape)
        {
            float length = 0.5f - shape.Half;
            float offset = shape.Half + (length * 0.5f);
            bool alongZ = direction is Direction.North or Direction.South;
            float sign = direction is Direction.North or Direction.East ? 1f : -1f;
            Vector3 center = alongZ ? new Vector3(0f, shape.Height * 0.5f, sign * offset) : new Vector3(sign * offset, shape.Height * 0.5f, 0f);
            Vector3 size = alongZ ? new Vector3(shape.Half * 2f, shape.Height, length) : new Vector3(length, shape.Height, shape.Half * 2f);
            return (center, size);
        }

        private static bool SetupDoor(GameObject root)
        {
            bool changed = false;
            foreach ((Transform leaf, Bounds bounds) in Leaves(root))
            {
                if (LeafDrift(leaf, bounds) == null)
                    continue;

                foreach (Transform old in leaf.Cast<Transform>().Where(child => child.name == HolderName).ToList())
                    Object.DestroyImmediate(old.gameObject);

                GameObject holder = NewChild(HolderName, leaf);
                holder.AddComponent<BoxCollider>().center = bounds.center;
                holder.GetComponent<BoxCollider>().size = bounds.size;
                changed = true;
            }

            return changed;
        }

        private static bool RemoveDoorOccluders(GameObject root)
        {
            List<Transform> holders = DoorHolders(root).ToList();
            foreach (Transform holder in holders)
                Object.DestroyImmediate(holder.gameObject);

            return holders.Count > 0;
        }

        private static IEnumerable<Transform> DoorHolders(GameObject root) =>
            root.GetComponentsInChildren<Transform>(true).Where(t => t.name == HolderName);

        private static IEnumerable<string> DoorDrift(GameObject root)
        {
            List<(Transform Leaf, Bounds Bounds)> leaves = Leaves(root).ToList();
            if (leaves.Count == 0)
                yield return $"no door leaves (a non-trigger box with a mesh under the root, or a {BlockerName} box)";

            foreach ((Transform leaf, Bounds bounds) in leaves)
            {
                string drift = LeafDrift(leaf, bounds);
                if (drift != null)
                    yield return $"{leaf.name} {drift}";
            }
        }

        private static string LeafDrift(Transform leaf, Bounds bounds)
        {
            Transform[] holders = leaf.Cast<Transform>().Where(child => child.name == HolderName).ToArray();
            if (holders.Length != 1)
                return "has no single occluder child";

            Transform holder = holders[0];
            if (holder.localPosition != Vector3.zero || holder.localRotation != Quaternion.identity || holder.localScale != Vector3.one)
                return "occluder is moved off its leaf";

            return BoxDrift(holder, bounds.center, bounds.size, true);
        }

        /// <summary>
        /// Door leaves: direct children with a solid box and a mesh, i.e. what slides open, and any solid
        /// <see cref="BlockerName"/> box, which stands in for a skinned leaf and rides its bone (the external airlock).
        /// </summary>
        private static IEnumerable<(Transform Leaf, Bounds Bounds)> Leaves(GameObject root)
        {
            foreach (Transform blocker in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == BlockerName))
            {
                BoxCollider box = blocker.GetComponent<BoxCollider>();
                if (box != null && !box.isTrigger && blocker.gameObject.layer == 0)
                    yield return (blocker, new Bounds(box.center, box.size));
            }

            foreach (Transform child in root.transform)
            {
                if (child.name == HolderName || child.gameObject.layer != 0)
                    continue;

                BoxCollider box = child.GetComponent<BoxCollider>();
                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (box == null || box.isTrigger || filter == null || filter.sharedMesh == null)
                    continue;

                yield return (child, filter.sharedMesh.bounds);
            }
        }

        private static string BoxDrift(Transform owner, Vector3 center, Vector3 size, bool active)
        {
            if (owner == null)
                return "missing";

            BoxCollider box = owner.GetComponent<BoxCollider>();
            if (box == null || box.isTrigger)
                return "has no solid box";
            if (owner.gameObject.layer != VisionOccluder.Layer)
                return "is off the VisionOccluder layer";
            if (owner.gameObject.activeSelf != active)
                return active ? "is inactive" : "is active in the prefab";
            if ((box.center - center).sqrMagnitude > Tolerance * Tolerance || (box.size - size).sqrMagnitude > Tolerance * Tolerance)
                return $"box {box.center}/{box.size} should be {center}/{size}";

            return null;
        }

        private static IEnumerable<Transform> Holders(GameObject root) =>
            root.transform.Cast<Transform>().Where(child => child.name == HolderName);

        private static bool RemoveLegacyObstacles(GameObject root)
        {
            List<Transform> legacy = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.EndsWith(LegacyObstacleSuffix))
                .ToList();
            foreach (Transform obstacle in legacy)
                Object.DestroyImmediate(obstacle.gameObject);

            return legacy.Count > 0;
        }

        private static GameObject NewChild(string name, Transform parent)
        {
            GameObject child = new(name) { layer = VisionOccluder.Layer };
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject AddBox(GameObject holder, string name, Vector3 center, Vector3 size, bool active)
        {
            GameObject box = NewChild(name, holder.transform);
            BoxCollider collider = box.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            box.SetActive(active);
            return box;
        }

        private const string HolderName = VisionOccluder.HolderName;
        private const string BlockerName = "Blocker";
    }
}
#endif
