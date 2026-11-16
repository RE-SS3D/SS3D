using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Furniture;
using SS3D.Systems.Screens;
using SS3D.Systems.Vision;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SS3D.Tests
{
    /// <summary>
    /// Before/after pictures and cost of the FOV on Classic MiniStation (2026-11_fov-rework P0, P2, P3):
    /// a doorway and the wall corner beside it at 10°, 45° and 80° camera tilt, and the
    /// <c>Vision.*</c> profiler markers averaged over a few seconds. Writes to
    /// <c>artifacts/ui/vision-shots-&lt;UTC&gt;/</c>. Explicit: run with <c>validate.sh --steps scenarios:VisionShots</c>.
    /// </summary>
    [Explicit("Captures pictures for review; asserts nothing about them")]
    [Category("VisionShots")]
    public class VisionShots : ScenarioFixture
    {
        private static readonly float[] Tilts = { 10f, 45f, 80f };
        private static readonly string[] Markers = { "Vision.ViewPoints", "Vision.VisionMap", "Vision.Cull" };

        protected override string StationFixture => "Builds/Game/Data/StationTemplates/classic-ministation.json";

        protected override bool ChecksRoomAir => false;

        // Guards: nothing; it records pictures and timings for the owner to compare · Not cheaper because: the pictures are the product
        [UnityTest]
        public IEnumerator Capture()
        {
            VisionSubSystem vision = SubSystems.Get<VisionSubSystem>();
            Assume.That(vision != null && vision.viewPoints.IsCreated, "FOV needs a graphics device");
            CameraFollow follow = Object.FindFirstObjectByType<CameraFollow>();
            Assert.IsNotNull(follow, "The game camera follows the body");

            string output = Path.GetFullPath("artifacts/ui/vision-shots-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output);

            Vector3 start = Body.transform.position;
            AirLockOpener door = Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None)
                .OrderBy(candidate => (candidate.transform.position - start).sqrMagnitude)
                .First();
            Transform doorTransform = door.transform;
            Vector3 through = Flat(doorTransform.forward);
            Vector3 along = Flat(doorTransform.right);
            float height = start.y;

            Vector3 doorway = doorTransform.position + (through * 1.6f);
            Vector3 corner = doorTransform.position + (through * 0.75f) + (along * 1.2f);

            StringBuilder report = new();
            report.AppendLine($"door {door.name} at {doorTransform.position}, through {through}");

            // Let the round-start announcement clear the top of the screen.
            yield return Hold(6f);
            yield return Shoot(vision, follow, doorway, height, through, "doorway", output);
            yield return Shoot(vision, follow, corner, height, through + (along * 0.6f), "corner", output);
            report.Append(Diagnose(vision, doorTransform.position, height));

            Teleport(Body, new Vector3(doorway.x, height, doorway.z));
            yield return Hold(0.5f);
            yield return Profile(120, report);

            File.WriteAllText(Path.Combine(output, "report.md"), report.ToString());
            Debug.Log($"[VisionShots] wrote {output}");
        }

        private IEnumerator Shoot(VisionSubSystem vision, CameraFollow follow, Vector3 stand, float height, Vector3 towardCamera, string label, string output)
        {
            Teleport(Body, new Vector3(stand.x, height, stand.z));
            towardCamera = Flat(towardCamera);
            // CameraFollow puts the camera at Euler(0, h, v) * +X, i.e. horizontally (cos h, -sin h).
            float horizontal = Mathf.Atan2(-towardCamera.z, towardCamera.x) * Mathf.Rad2Deg;
            foreach (float tilt in Tilts)
            {
                SetField(follow, "_horizontalAngle", horizontal);
                SetField(follow, "_verticalAngle", tilt);
                yield return Hold(1.2f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, $"{label}-{tilt:0}.png"));
                yield return null;
                yield return null;

                // The same view with the FOV off, to tell what the black covers.
                vision.SetSuppressed(true);
                yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, $"{label}-{tilt:0}-open.png"));
                yield return null;
                vision.SetSuppressed(false);
                yield return null;
            }
        }

        private static IEnumerator Profile(int frames, StringBuilder report)
        {
            ProfilerRecorder[] recorders = Markers
                .Select(marker => ProfilerRecorder.StartNew(ProfilerCategory.Scripts, marker))
                .ToArray();
            double[] sum = new double[recorders.Length];
            double[] max = new double[recorders.Length];
            yield return null;
            for (int frame = 0; frame < frames; frame++)
            {
                yield return null;
                for (int r = 0; r < recorders.Length; r++)
                {
                    double ms = recorders[r].LastValue / 1e6;
                    sum[r] += ms;
                    max[r] = Math.Max(max[r], ms);
                }
            }

            report.AppendLine();
            report.AppendLine($"| Marker | Mean ms over {frames} frames | Max ms |");
            report.AppendLine("|---|---|---|");
            for (int r = 0; r < recorders.Length; r++)
            {
                report.AppendLine($"| {Markers[r]} | {sum[r] / frames:F3} | {max[r]:F3} |");
                recorders[r].Dispose();
            }
        }

        /// <summary>What the rays meet toward the door from where the body stands, for reading a picture.</summary>
        private static string Diagnose(VisionSubSystem vision, Vector3 door, float floor)
        {
            StringBuilder text = new();
            Vector3 eye = vision.DetectionCenter;
            text.AppendLine($"\nstanding at {eye}, state {vision.State}");
            foreach (Vector3 offset in new[] { Vector3.zero, Vector3.right * 0.3f, Vector3.left * 0.3f })
            {
                Vector3 aim = door + offset;
                Vector3 direction = Flat(aim - eye);
                bool hit = Physics.Raycast(eye, direction, out RaycastHit info, 35f, SS3D.Systems.Tile.Connections.VisionOccluder.Mask, QueryTriggerInteraction.Ignore);
                text.AppendLine($"toward door{offset}: depth {vision.DepthAlong(direction):F2}; ray hits {(hit ? info.collider.name + " at " + info.point + " n " + info.normal + " tile " + VisionSubSystem.HitTile(info.point, info.normal) : "nothing")}");
            }

            foreach (float y in new[] { 0.5f, 1.5f, 2.2f })
                text.AppendLine($"door tile at {y} m visible: {vision.IsVisible(new Vector3(door.x, floor + y, door.z))}");
            return text.ToString();
        }

        private static Vector3 Flat(Vector3 vector)
        {
            vector.y = 0f;
            return vector.normalized;
        }

        private static void SetField(object target, string name, float value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"CameraFollow.{name} exists");
            field.SetValue(target, value);
        }
    }
}
