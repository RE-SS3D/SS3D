using NUnit.Framework;
using SS3D.Core;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The airlock doors stop at about 1.93 m and the station ceiling starts at 2.5 m. A lintel collider on the
    /// ceiling layer fills that gap, so a thrown item cannot sail over a closed airlock.
    /// </summary>
    public class AirlockLintelTests
    {
        private const string DoorsFolder = "Assets/Content/WorldObjects/Structures/Doors/";

        [TestCase("CivillianAirlock.prefab")]
        [TestCase("SecurityAirlock.prefab")]
        [TestCase("CivillianAirlockDouble.prefab")]
        public void Lintel_FillsTheGapFromTheDoorTopsToTheCeiling(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorsFolder + prefabName);
            Assert.IsNotNull(prefab, prefabName);

            Bounds? doors = null;
            Bounds? lintel = null;
            foreach (BoxCollider box in prefab.GetComponentsInChildren<BoxCollider>(true))
            {
                if (box.isTrigger)
                {
                    continue;
                }

                Bounds local = RootLocalBounds(prefab.transform, box);
                if (StationBounds.IsCeiling(box))
                {
                    lintel = Encapsulate(lintel, local);
                }
                else
                {
                    doors = Encapsulate(doors, local);
                }
            }

            Assert.IsTrue(doors.HasValue, $"{prefabName}: no door colliders");
            Assert.IsTrue(lintel.HasValue, $"{prefabName}: no lintel collider on the {StationBounds.CeilingLayerName} layer");

            Bounds d = doors.Value;
            Bounds l = lintel.Value;
            Assert.LessOrEqual(l.min.y, d.max.y, $"{prefabName}: gap between the door tops and the lintel");
            Assert.GreaterOrEqual(l.max.y, StationBounds.CeilingHeight, $"{prefabName}: gap between the lintel and the ceiling");
            Assert.LessOrEqual(l.min.x, d.min.x + 0.01f, $"{prefabName}: lintel narrower than the doorway");
            Assert.GreaterOrEqual(l.max.x, d.max.x - 0.01f, $"{prefabName}: lintel narrower than the doorway");
            Assert.Greater(l.min.y, 1.5f, $"{prefabName}: lintel low enough to catch a walking character");
        }

        private static Bounds Encapsulate(Bounds? current, Bounds add)
        {
            if (!current.HasValue)
            {
                return add;
            }

            Bounds bounds = current.Value;
            bounds.Encapsulate(add);
            return bounds;
        }

        private static Bounds RootLocalBounds(Transform root, BoxCollider box)
        {
            Vector3 half = box.size * 0.5f;
            Bounds bounds = new(root.InverseTransformPoint(box.transform.TransformPoint(box.center)), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = box.center + Vector3.Scale(half,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                bounds.Encapsulate(root.InverseTransformPoint(box.transform.TransformPoint(corner)));
            }

            return bounds;
        }
    }
}
