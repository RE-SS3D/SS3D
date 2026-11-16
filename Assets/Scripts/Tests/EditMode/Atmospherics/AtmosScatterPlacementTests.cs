using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Rendering.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EditorTests
{
    /// <summary>
    /// Renders a synthetic gas atlas through the real <see cref="AtmosRendererFeature"/> and checks where the
    /// fog lands on screen. Guards issue #125: gas drawn mirrored across the screen's horizontal centre line,
    /// which put clouds in empty space and upside-down silhouettes in the fog.
    /// Skipped without a graphics device, so it runs locally and not in the headless CI job.
    /// </summary>
    public class AtmosScatterPlacementTests
    {
        private const int Width = 640;
        private const int Height = 480;
        private const int AtlasSize = 40;
        private const float AtlasMin = -20f;

        /// <summary>Blue minus green. The test gas is pure magenta and the scene has no other magenta.</summary>
        private const float VisibleTint = 0.15f;

        private readonly List<Object> _created = new();
        private RenderTexture _target;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Needs a graphics device to render.");

            _target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            _target.Create();

            GameObject cameraObject = Track(new GameObject("AtmosScatterTestCamera"));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            _camera.enabled = false;
            _camera.fieldOfView = 40f;
            _camera.aspect = (float)Width / Height;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.transform.position = new Vector3(0f, 12f, -9f);
            _camera.transform.LookAt(Vector3.zero);
            _camera.targetTexture = _target;
        }

        [TearDown]
        public void TearDown()
        {
            AtmosRenderContext.ClearRequest();
            AtmosRenderContext.ClearSnapshot();
            foreach (Object created in _created)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }

            _created.Clear();
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }
        }

        [Test]
        public void GasOnTheStation_IsDrawnOverItsTiles_NotOutInSpace()
        {
            // The floor stops just past the view centre, so the top of the frame is empty space.
            CreateBox(new Vector3(0f, -0.5f, -9.5f), new Vector3(60f, 1f, 21f), Color.gray);

            Texture2D frame = Render(CreateGasSnapshot(-3f, 3f, -7f, -3f));

            Vector3 gas = _camera.WorldToViewportPoint(new Vector3(0f, 0f, -5f));
            Assert.Greater(Tint(frame, gas.x, gas.y), VisibleTint, "Gas should show over its own tiles");
            Assert.Less(Tint(frame, gas.x, 1f - gas.y), VisibleTint, "Gas should not show in space at the mirrored pixel");
        }

        [Test]
        public void ObjectInGas_LeavesNoMirroredHoleInTheFog()
        {
            CreateBox(new Vector3(0f, -0.5f, 0f), new Vector3(100f, 1f, 100f), Color.gray);
            Vector3 pillar = new(0f, 1.25f, -5f);
            CreateBox(pillar, new Vector3(1.5f, 2.5f, 1.5f), Color.green);

            Texture2D frame = Render(CreateGasSnapshot(-20f, 20f, -20f, 20f));

            Vector3 viewport = _camera.WorldToViewportPoint(new Vector3(pillar.x, 0f, pillar.z));
            float mirrored = Tint(frame, viewport.x, 1f - viewport.y);
            float beside = Tint(frame, viewport.x + 0.25f, 1f - viewport.y);
            Assert.AreEqual(beside, mirrored, 0.05f, "Fog should be even across the pillar's mirrored position");
        }

        private static float Tint(Texture2D frame, float viewportX, float viewportY)
        {
            Color color = frame.GetPixel((int)(viewportX * Width), (int)(viewportY * Height));
            return color.b - color.g;
        }

        private Texture2D Render(AtmosRenderContext.Snapshot snapshot)
        {
            AtmosRenderContext.SetSnapshot(snapshot);
            AtmosRenderContext.SetRequest(new AtmosRenderContext.Request { SourceCamera = _camera });
            _camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            Texture2D frame = Track(new Texture2D(Width, Height, TextureFormat.RGBA32, false));
            frame.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            frame.Apply();
            RenderTexture.active = previous;
            return frame;
        }

        private AtmosRenderContext.Snapshot CreateGasSnapshot(float minX, float maxX, float minZ, float maxZ)
        {
            Texture2D pressure = CreateAtlas(TextureFormat.RFloat, FilterMode.Bilinear);
            Texture2D temperature = CreateAtlas(TextureFormat.RFloat, FilterMode.Bilinear);
            Texture2D composition = CreateAtlas(TextureFormat.RGBA32, FilterMode.Bilinear);
            Texture2D flow = CreateAtlas(TextureFormat.RGFloat, FilterMode.Bilinear);
            Texture2D fire = CreateAtlas(TextureFormat.RFloat, FilterMode.Bilinear);
            Texture2D mask = CreateAtlas(TextureFormat.R8, FilterMode.Point);

            for (int z = 0; z < AtlasSize; z++)
            {
                for (int x = 0; x < AtlasSize; x++)
                {
                    float worldX = AtlasMin + x;
                    float worldZ = AtlasMin + z;
                    bool hasGas = worldX >= minX && worldX <= maxX && worldZ >= minZ && worldZ <= maxZ;

                    pressure.SetPixel(x, z, new Color(101.3f, 0f, 0f, 0f));
                    temperature.SetPixel(x, z, new Color(293f, 0f, 0f, 0f));
                    composition.SetPixel(x, z, new Color(0f, 0f, 0f, hasGas ? 1f : 0f));
                    flow.SetPixel(x, z, new Color(0.5f, 0.5f, 0f, 0f));
                    fire.SetPixel(x, z, Color.clear);
                    mask.SetPixel(x, z, new Color((hasGas ? 1 : 2) / 255f, 0f, 0f, 0f));
                }
            }

            pressure.Apply();
            temperature.Apply();
            composition.Apply();
            flow.Apply();
            fire.Apply();
            mask.Apply();

            Vector4[] scatter = new Vector4[4];
            scatter[3] = new Vector4(1f, 0f, 1f, 1.5f);
            return new AtmosRenderContext.Snapshot
            {
                Pressure = pressure,
                Temperature = temperature,
                Composition = composition,
                Flow = flow,
                FireIntensity = fire,
                Mask = mask,
                AtlasBounds = new Vector4(AtlasMin, AtlasMin, AtlasSize, AtlasSize),
                Valid = true,
                IgnitionTemperature = 1000f,
                GasScatter = scatter,
                GasEmission = new Vector4[4],
                GasMisc = new Vector4[4],
            };
        }

        private Texture2D CreateAtlas(TextureFormat format, FilterMode filter)
        {
            Texture2D texture = Track(new Texture2D(AtlasSize, AtlasSize, format, false, true));
            texture.filterMode = filter;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private void CreateBox(Vector3 position, Vector3 scale, Color color)
        {
            GameObject box = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            box.hideFlags = HideFlags.HideAndDontSave;
            box.transform.position = position;
            box.transform.localScale = scale;

            Material material = Track(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            material.SetColor("_BaseColor", color);
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private T Track<T>(T created)
            where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
