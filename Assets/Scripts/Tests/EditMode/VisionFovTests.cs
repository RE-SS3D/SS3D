using NUnit.Framework;
using SS3D.Systems.Vision;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The CPU half of the FOV rule (systems/fov.md Rule + Cast): gameplay asks <see cref="VisionSubSystem.IsVisible"/>
    /// the same question the mask shader answers, so these pin the depth test, the hit tile and the wall rule.
    /// </summary>
    public class VisionFovTests
    {
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
        public void HitTile_IsTheTileBehindTheSurface()
        {
            // A ray from the west hits a wall arm's face on the tile edge at x = 4.5: the tile is the wall's (5), not the floor's (4).
            Assert.AreEqual(new Vector2Int(5, 2), VisionSubSystem.HitTile(new Vector3(4.5f, 1.5f, 2.2f), Vector3.left));
            // The same edge hit from the east lands in tile 4.
            Assert.AreEqual(new Vector2Int(4, 2), VisionSubSystem.HitTile(new Vector3(4.5f, 1.5f, 2.2f), Vector3.right));
            // An arm face inside its tile.
            Assert.AreEqual(new Vector2Int(-3, 7), VisionSubSystem.HitTile(new Vector3(-3.216f, 1.5f, 6.8f), Vector3.left));
        }

        private const int TileGrid = 5;
        private static readonly Vector2Int GridOrigin = new(-2, -2);

        private static byte[] SeenTiles(params Vector2Int[] tiles)
        {
            byte[] seen = new byte[TileGrid * TileGrid];
            foreach (Vector2Int tile in tiles)
                seen[(tile.x - GridOrigin.x) + ((tile.y - GridOrigin.y) * TileGrid)] = 255;
            return seen;
        }

        [Test]
        public void SeenWall_ShowsWhole_AboveTheFloor()
        {
            byte[] seen = SeenTiles(new Vector2Int(1, 0));

            Assert.IsTrue(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 0f, new Vector3(1.3f, 1.2f, 0.4f)),
                "Any part of a wall tile a ray hit shows, its far face too");
            Assert.IsTrue(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 0f, new Vector3(0.6f, 2.5f, -0.4f)),
                "And its top");
            Assert.IsFalse(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 0f, new Vector3(1.3f, 0.02f, 0f)),
                "Its floor keeps the depth test, so the strip behind a door leaf stays hidden");
            Assert.IsFalse(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 0f, new Vector3(2f, 1.2f, 0f)),
                "A wall no ray hit stays hidden");
        }

        [Test]
        public void SeenWall_HeightIsFromTheBodysFloor()
        {
            byte[] seen = SeenTiles(new Vector2Int(0, 1));

            Assert.IsFalse(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 3f, new Vector3(0f, 3.05f, 1f)));
            Assert.IsTrue(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 3f, new Vector3(0f, 3.5f, 1f)));
        }

        [Test]
        public void SeenWall_OutsideTheGrid_IsHidden()
        {
            byte[] seen = SeenTiles(new Vector2Int(2, 2));

            Assert.IsFalse(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 0f, new Vector3(3f, 1f, 2f)));
            Assert.IsFalse(VisionSubSystem.IsPartOfSeenWall(seen, TileGrid, GridOrigin, 0f, new Vector3(-3f, 1f, -3f)));
        }
    }
}
