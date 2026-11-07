using NUnit.Framework;
using SS3D.Systems.Vision;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The FOV lights a little past each wall hit so the wall's own mesh stays visible. That slack
    /// has to stop before the far face the wall draws, or what is behind it shows (issue #145).
    /// Gameplay reads the same depths through IsWithinDepths, so examine agrees with the black mask.
    /// </summary>
    public class VisionFovTests
    {
        // Measured from the near collider face to the far face the mesh draws, not the far collider
        // face: colliders fill the tile, while the meshes above the wall's flared base do not.
        private const float WallFarFace = 0.72f; // SteelWallI faces sit 0.22 m from the tile centre.
        private const float AirlockFarFace = 0.57f; // Door faces sit 0.17 m from the tile centre.

        // SteelWallI's main face sits 0.28 m behind the collider.
        private const float WallFaceRecess = 0.28f;

        // Total slack before this fix, which stayed clear of glancing-angle wall blackout.
        private const float PreviousTotalSlack = 1.15f;

        private static readonly Vector3 WallNormal = Vector3.back;

        private static float TotalSlack(float degreesFromPerpendicular)
        {
            Vector3 ray = Quaternion.AngleAxis(degreesFromPerpendicular, Vector3.up) * Vector3.forward;
            return VisionSubSystem.OccluderSlack(ray, WallNormal) + VisionSubSystem.ShaderDepthSlack;
        }

        private static float DepthReachedIntoWall(float degreesFromPerpendicular)
        {
            return TotalSlack(degreesFromPerpendicular) * Mathf.Cos(degreesFromPerpendicular * Mathf.Deg2Rad);
        }

        [Test]
        public void Slack_NeverReachesTheFarFaceOfAWallOrAirlock(
            [NUnit.Framework.Range(0f, 89f, 1f)] float degrees)
        {
            Assert.Less(DepthReachedIntoWall(degrees), WallFarFace);
            Assert.Less(DepthReachedIntoWall(degrees), AirlockFarFace);
        }

        [Test]
        public void Slack_NeverExceedsThePreviousConstant([NUnit.Framework.Range(0f, 89f, 1f)] float degrees)
        {
            Assert.LessOrEqual(TotalSlack(degrees), PreviousTotalSlack + 1e-4f);
        }

        [Test]
        public void Slack_MatchesThePreviousConstant_WhereRecessedFacesNeedIt(
            [Values(70f, 75f, 85f)] float degrees)
        {
            Assert.AreEqual(PreviousTotalSlack, TotalSlack(degrees), 1e-4f);
        }

        [Test]
        public void Slack_KeepsRecessedWallFacesLit_WhereItWasReduced(
            [NUnit.Framework.Range(0f, 70f, 1f)] float degrees)
        {
            float rayLengthToFace = WallFaceRecess / Mathf.Cos(degrees * Mathf.Deg2Rad);
            Assert.Greater(TotalSlack(degrees), rayLengthToFace);
        }

        // An airlock's jamb cap: 0.05 m along a wall that runs along x, the full tile through it.
        private static readonly Vector3 JambCapSize = new(0.05f, 2.6f, 1f);

        [Test]
        public void OcclusionNormal_KeepsTheHitNormal_OnAWall()
        {
            Vector3 normal = VisionSubSystem.OcclusionNormal(Vector3.left, new Vector3(1f, 2.6f, 1f));

            Assert.AreEqual(Vector3.left, normal);
        }

        [Test]
        public void OcclusionNormal_OnAJambCapsSide_MeasuresThroughTheWall()
        {
            Vector3 normal = VisionSubSystem.OcclusionNormal(Vector3.left, JambCapSize);

            Assert.AreEqual(0f, Vector3.Dot(normal, Vector3.left), 1e-4f);
            Assert.AreEqual(1f, Mathf.Abs(normal.z), 1e-4f);
        }

        // From the door's recess a ray hits the cap's side, whose normal runs along the wall, and
        // goes on through the wall's thickness. Measured along that normal it crossed the wall.
        [Test]
        public void Slack_PastAJambCapsSide_StopsBeforeTheFarFace([NUnit.Framework.Range(10f, 40f, 2f)] float degrees)
        {
            Vector3 ray = Quaternion.AngleAxis(degrees, Vector3.up) * Vector3.forward;
            Vector3 normal = VisionSubSystem.OcclusionNormal(Vector3.left, JambCapSize);
            float slack = VisionSubSystem.OccluderSlack(ray, normal) + VisionSubSystem.ShaderDepthSlack;

            Assert.Less(slack * ray.z, WallFarFace);
        }

        // Standing beside a door, a ray glancing along the wall stops on the cap's side, with the
        // wall beside the door behind it. It keeps the full slack, or that wall paints black.
        [Test]
        public void Slack_AlongTheWallIntoAJambCap_KeepsTheWallBesideTheDoorLit()
        {
            Vector3 ray = Quaternion.AngleAxis(-78f, Vector3.up) * Vector3.forward;
            Vector3 normal = VisionSubSystem.OcclusionNormal(Vector3.right, JambCapSize);

            Assert.AreEqual(PreviousTotalSlack, VisionSubSystem.OccluderSlack(ray, normal) + VisionSubSystem.ShaderDepthSlack, 1e-4f);
        }

        private const float Range = 35f;

        private static bool IsVisible(float[] depths, Vector3 point, float coneWidth = 360f, float yaw = 0f)
        {
            return VisionSubSystem.IsWithinDepths(depths, depths.Length, Range, coneWidth, Vector3.zero, yaw, point);
        }

        private static float[] OpenExceptAhead(float metersAhead)
        {
            float[] depths = new float[360];
            for (int i = 0; i < depths.Length; i++)
                depths[i] = 1f;

            // A 360 degree cone starts behind the viewer, so bin 180 looks along yaw.
            depths[179] = depths[180] = metersAhead / Range;
            return depths;
        }

        [Test]
        public void IsWithinDepths_HidesAPointPastTheWallAhead()
        {
            float[] depths = OpenExceptAhead(3f);

            Assert.IsTrue(IsVisible(depths, new Vector3(0f, 0f, 2.9f)));
            Assert.IsFalse(IsVisible(depths, new Vector3(0f, 0f, 3.5f)));
        }

        [Test]
        public void IsWithinDepths_LeavesOtherDirectionsAlone()
        {
            float[] depths = OpenExceptAhead(3f);

            Assert.IsTrue(IsVisible(depths, new Vector3(0f, 0f, -10f)));
            Assert.IsTrue(IsVisible(depths, new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void IsWithinDepths_IgnoresHeight()
        {
            Assert.IsFalse(IsVisible(OpenExceptAhead(3f), new Vector3(0f, 9f, 3.5f)));
        }

        [Test]
        public void IsWithinDepths_FollowsYaw()
        {
            float[] depths = OpenExceptAhead(3f);

            Assert.IsFalse(IsVisible(depths, new Vector3(3.5f, 0f, 0f), yaw: 90f));
            Assert.IsTrue(IsVisible(depths, new Vector3(0f, 0f, 3.5f), yaw: 90f));
        }

        [Test]
        public void IsWithinDepths_OutsideANarrowCone_ShowsOnlyTheNearBubble()
        {
            float[] depths = { 1f, 1f, 1f, 1f };

            Assert.IsTrue(IsVisible(depths, new Vector3(0f, 0f, 5f), coneWidth: 90f));
            Assert.IsFalse(IsVisible(depths, new Vector3(0f, 0f, -5f), coneWidth: 90f));
            Assert.IsTrue(IsVisible(depths, new Vector3(0f, 0f, -0.5f), coneWidth: 90f));
        }

        [Test]
        public void Slack_FallsBackToTheCap_WithoutAHitNormal()
        {
            float slack = VisionSubSystem.OccluderSlack(Vector3.forward, Vector3.zero);

            Assert.AreEqual(PreviousTotalSlack, slack + VisionSubSystem.ShaderDepthSlack, 1e-4f);
        }
    }
}
