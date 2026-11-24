using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Screens;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: the cursor's hover target (examine, outlines, interactions) is the floor, wall or item under it, also
    // after renderer rebuilds (quality changes) · Not cheaper because: the pick is a URP render pass over the
    // live station renderers, read back through the player camera and the selection system
    [Category(TestCategories.Scenarios)]
    public class SelectionHoverScenarios : ScenarioFixture
    {
        private const float OverviewDistance = 9f;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CameraDistance = typeof(CameraFollow).GetField("_cameraDistance", Private);
        private static readonly FieldInfo CurrentDistance = typeof(CameraFollow).GetField("_currentDistance", Private);

        [UnityTest]
        public IEnumerator Hover_FindsFloorWallAndItem_AcrossQualityChanges()
        {
            int quality = QualitySettings.GetQualityLevel();
            Mouse mouse = InputSystem.AddDevice<Mouse>("SelectionHoverScenarioMouse");
            CameraFollow follow = Object.FindFirstObjectByType<CameraFollow>();
            Assert.IsNotNull(follow, "The host's camera follows the body");
            float distance = (float)CameraDistance.GetValue(follow);
            try
            {
                // Zoom out from the default so wall tops are on screen as well as the floor around the body.
                CameraDistance.SetValue(follow, OverviewDistance);
                CurrentDistance.SetValue(follow, OverviewDistance);
                yield return Hold(0.3f);

                Camera camera = Camera.main;
                Assert.IsNotNull(camera, "The host has a player camera");
                Vector3 body = Body.transform.position;

                Item item = SubSystems.Get<ItemSubSystem>().SpawnItem(Items.ToolboxBlue,
                    body + (camera.transform.right * 1.2f), Quaternion.identity);
                yield return Hold(0.5f);

                PlacedTileObject[] tiles = Object.FindObjectsByType<PlacedTileObject>(FindObjectsSortMode.None);
                PlacedTileObject floor = Nearest(tiles, TileObjectGenericType.Floor, camera,
                    body - (camera.transform.right * 1.5f), body);
                PlacedTileObject wall = Nearest(tiles, TileObjectGenericType.Wall, camera, body, body);
                Assert.IsNotNull(floor, "A floor tile is on screen: " + Describe(tiles, TileObjectGenericType.Floor, camera, body));
                Assert.IsNotNull(wall, "A wall is on screen: " + Describe(tiles, TileObjectGenericType.Wall, camera, body));

                // Each quality change rebuilds the renderer: every renderer feature is disposed and created again.
                // Without the fix the pick material dies on one rebuild and is recreated on the next, and Play may
                // already have started with a rebuild (PlayerDisplayApplier), so check after two in a row.
                int[] levels = { (quality + 1) % QualitySettings.names.Length, quality };
                foreach (int level in levels)
                {
                    QualitySettings.SetQualityLevel(level, true);
                    yield return Hold(0.3f);
                    string tier = QualitySettings.names[level];

                    yield return ExpectHover(mouse, camera, floor.gameObject, $"the floor on {tier}");
                    yield return ExpectHover(mouse, camera, wall.gameObject, $"a wall on {tier}");
                    yield return ExpectHover(mouse, camera, item.gameObject, $"an item on {tier}");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(quality, true);
                InputSystem.RemoveDevice(mouse);
                CameraDistance.SetValue(follow, distance);
            }
        }

        // The on-screen tile of this kind whose top centre is nearest `near`, skipping the tile the body stands on
        // (the body covers it, so the pick correctly returns the body).
        private static PlacedTileObject Nearest(PlacedTileObject[] tiles, TileObjectGenericType type, Camera camera,
            Vector3 near, Vector3 body)
        {
            return tiles
                .Where(tile => tile.GenericType == type && tile.GetComponentInChildren<Selectable>() != null)
                .Where(tile => OnScreen(camera, TopCentre(tile.gameObject)))
                .Where(tile => !UnderBody(tile, body))
                .OrderBy(tile => (TopCentre(tile.gameObject) - near).sqrMagnitude)
                .FirstOrDefault();
        }

        private static bool UnderBody(PlacedTileObject tile, Vector3 body)
        {
            Vector3 offset = tile.transform.position - body;
            return Mathf.Abs(offset.x) < 0.75f && Mathf.Abs(offset.z) < 0.75f;
        }

        private static string Describe(PlacedTileObject[] tiles, TileObjectGenericType type, Camera camera, Vector3 near)
        {
            PlacedTileObject[] ofType = tiles.Where(tile => tile.GenericType == type).ToArray();
            PlacedTileObject nearest = ofType.OrderBy(tile => (TopCentre(tile.gameObject) - near).sqrMagnitude)
                .FirstOrDefault();
            return nearest == null
                ? $"none of {tiles.Length} tile objects is a {type}"
                : $"{ofType.Length} of type {type}, nearest {nearest.name} at viewport "
                + $"{camera.WorldToViewportPoint(TopCentre(nearest.gameObject))}, selectable "
                + (nearest.GetComponentInChildren<Selectable>() != null);
        }

        private static IEnumerator ExpectHover(Mouse mouse, Camera camera, GameObject target, string what)
        {
            Vector2 screen = camera.WorldToScreenPoint(TopCentre(target));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            InputSystem.Update();
            mouse.MakeCurrent();

            // The pick is read back asynchronously, so hover trails the cursor by a few frames.
            SelectionSubSystem selection = SubSystems.Get<SelectionSubSystem>();
            float start = Time.time;
            Selectable hovered = selection.GetCurrentSelectable();
            while (hovered == null || !hovered.transform.IsChildOf(target.transform))
            {
                Assert.Less(Time.time - start, 3f,
                    $"Hovering {what} ({target.name}) at {screen} selects it, not {(hovered != null ? hovered.name : "nothing")}");
                yield return null;
                hovered = selection.GetCurrentSelectable();
            }
        }

        // The middle of the target's top face: what a top-down camera sees of a floor tile, a wall or an item.
        private static Vector3 TopCentre(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.enabled && renderer is MeshRenderer or SkinnedMeshRenderer)
                .ToArray();
            if (renderers.Length == 0)
            {
                return target.transform.position;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return new Vector3(bounds.center.x, bounds.max.y - 0.01f, bounds.center.z);
        }

        // Inside the middle of the screen, clear of the HUD along the edges.
        private static bool OnScreen(Camera camera, Vector3 world)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            return viewport.z > 0f && viewport.x is > 0.2f and < 0.8f && viewport.y is > 0.25f and < 0.8f;
        }
    }
}
