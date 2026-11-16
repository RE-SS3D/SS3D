using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Every wall mount's colliders sit on its visible panel. The light switch, air alarm and APC boxes were once
    /// authored at floor height inside the wall, so clicks, reach checks and rounds resolved against nothing visible.
    /// </summary>
    public class WallMountColliderTests
    {
        private const string WallMounts = "Assets/Content/WorldObjects/Structures/WallMounts";

        public static string[] AllWallMounts() => AssetDatabase.FindAssets("t:Prefab", new[] { WallMounts })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        [TestCaseSource(nameof(AllWallMounts))]
        public void CollidersSitOnThePanel(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (BoxCollider box in prefab.GetComponentsInChildren<BoxCollider>(true))
            {
                Bounds panel = default;
                bool any = false;
                foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null)
                    {
                        continue;
                    }

                    Bounds mesh = filter.sharedMesh.bounds;
                    Vector3 centre = box.transform.InverseTransformPoint(filter.transform.TransformPoint(mesh.center));
                    if (!any)
                    {
                        panel = new Bounds(centre, mesh.size);
                        any = true;
                    }
                    else
                    {
                        panel.Encapsulate(new Bounds(centre, mesh.size));
                    }
                }

                Assert.IsTrue(any, $"{path} has a collider but no mesh");
                Assert.IsTrue(new Bounds(box.center, box.size).Intersects(panel),
                    $"{path}: {box.name}'s collider (centre {box.center}) misses the panel drawn at {panel.center}. "
                    + "Run SS3D → Electricity → Run Content Prefab Recipes.");
            }
        }
    }
}
