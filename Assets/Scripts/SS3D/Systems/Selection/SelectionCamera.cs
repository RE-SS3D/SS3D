using SS3D.Core.Debugging;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Rendering.URP;
using SS3D.Systems.Inputs;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Selection
{
    /// <summary>
    /// Drives URP selection picking by registering a per-frame render request and reading
    /// the encoded selectable colour from an offscreen target after the player camera renders.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SelectionCamera : Actor
    {
        [SerializeField] private Shader _shader;

        private Camera _camera;
        private Camera _playerCamera;
        private RenderTexture _renderTexture;
        private Texture2D _readbackTexture;
        private SelectionSubSystem _system;
        private InputSubSystem _inputSystem;
        private System.Action<AsyncGPUReadbackRequest> _onReadback;
        private bool _readbackPending;
        private Color32 _latestPick = Color.black;
        private bool _debugMode;

        protected override void OnStart()
        {
            // PlayerCamera lives in Game before NetworkSystemsHub exists; Selection registers Online.
            SubSystems.TryGet(out _system);
            SubSystems.TryGet(out _inputSystem);
            _onReadback = OnPickReadback;
            _camera = GetComponent<Camera>();
            _playerCamera = transform.parent.GetComponent<Camera>();

            // The child camera existed for Built-in replacement-shader rendering only.
            _camera.enabled = false;
            _camera.targetTexture = null;

            EnsureRenderTextureSize();
            GenerateReadbackTexture();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            DebugTool tool = DebugTools.Get(DebugToolNames.SelectionBuffer);
            tool.Invoked += ToggleDebugMode;
            tool.IsOn = () => _debugMode;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            DebugTools.Get(DebugToolNames.SelectionBuffer).Invoked -= ToggleDebugMode;

            SelectionPickContext.ClearRequest();
        }

        protected override void OnDestroyed()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            SelectionPickContext.ClearRequest();

            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _playerCamera || _renderTexture == null)
            {
                return;
            }

            // Pointer over UI: skip the entire pick pass (hundreds of DrawMesh) — hover is cleared
            // in OnEndCameraRendering via black colour anyway.
            if (InputInterface.IsPointerOverInterface())
            {
                SelectionPickContext.ClearRequest();
                return;
            }

            EnsureRenderTextureSize();

            bool hasScreen = TryGetMouseScreenPosition(out Vector2 screenPosition);
            SelectionPickContext.SetRequest(new SelectionPickContext.Request
            {
                SourceCamera = _playerCamera,
                Target = _renderTexture,
                DebugView = _debugMode,
                ScreenPosition = screenPosition,
                HasScreenPosition = hasScreen,
            });
        }

        private void EnsureRenderTextureSize()
        {
            int width = Mathf.Max(1, _playerCamera != null ? _playerCamera.pixelWidth : Screen.width);
            int height = Mathf.Max(1, _playerCamera != null ? _playerCamera.pixelHeight : Screen.height);

            if (_renderTexture != null
                && _renderTexture.width == width
                && _renderTexture.height == height
                && _renderTexture.depth == 0)
            {
                return;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }

            // Color-only RT: depth testing reuses the player camera's active depth in the pick pass.
            _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                autoGenerateMips = false
            };

            GenerateReadbackTexture();
        }

        private void GenerateReadbackTexture()
        {
            if (_renderTexture == null)
            {
                return;
            }

            _readbackTexture = new Texture2D(1, 1, _renderTexture.graphicsFormat, TextureCreationFlags.None);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _playerCamera || _renderTexture == null || _readbackTexture == null)
            {
                return;
            }

            if (_system == null && !SubSystems.TryGet(out _system))
            {
                return;
            }

            // Selection feeds examine/outlines/interaction hover. Clear it while over UI so world
            // targets do not "show through" registered UI Toolkit panels (Main HUD, MI, radial).
            Color32 col = Color.black;
            if (!InputInterface.IsPointerOverInterface()
                && SelectionPickContext.TryGetRequest(out _)
                && TryGetMousePixel(out int x, out int y))
            {
                col = SystemInfo.supportsAsyncGPUReadback ? ReadPickAsync(x, y) : ReadPickNow(x, y);
            }
            else
            {
                _latestPick = Color.black;
            }

            _system.UpdateColourFromCamera(col);
        }

        /// <summary>
        /// A synchronous read of even one pixel makes the main thread wait for the GPU to finish the
        /// frame. This returns the newest finished read, so hover trails the cursor by a few frames.
        /// </summary>
        private Color32 ReadPickAsync(int x, int y)
        {
            if (!_readbackPending)
            {
                _readbackPending = true;
                AsyncGPUReadback.Request(_renderTexture, 0, x, 1, y, 1, 0, 1, _renderTexture.graphicsFormat, _onReadback);
            }

            return _latestPick;
        }

        private void OnPickReadback(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;
            if (request.hasError || _readbackTexture == null)
            {
                return;
            }

            // Same format as the target: picks are matched by exact colour, so decode as ReadPickNow does.
            _readbackTexture.LoadRawTextureData(request.GetData<byte>());
            _latestPick = _readbackTexture.GetPixel(0, 0);
        }

        /// <summary>Stalls on the GPU: only for platforms without async readback, such as headless players.</summary>
        private Color32 ReadPickNow(int x, int y)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            _readbackTexture.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
            _readbackTexture.Apply(false, false);
            RenderTexture.active = previous;
            return _readbackTexture.GetPixel(0, 0);
        }

        private bool TryGetMouseScreenPosition(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;
            if (_playerCamera == null)
            {
                return false;
            }

            Vector2 position = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;

            if (!_playerCamera.pixelRect.Contains(position))
            {
                return false;
            }

            screenPosition = position;
            return true;
        }

        private bool TryGetMousePixel(out int x, out int y)
        {
            x = 0;
            y = 0;

            if (!TryGetMouseScreenPosition(out Vector2 screenPosition))
            {
                return false;
            }

            Vector3 viewport = _playerCamera.ScreenToViewportPoint(screenPosition);
            x = Mathf.Clamp(Mathf.FloorToInt(viewport.x * _renderTexture.width), 0, _renderTexture.width - 1);
            y = Mathf.Clamp(Mathf.FloorToInt(viewport.y * _renderTexture.height), 0, _renderTexture.height - 1);
            return true;
        }

        public void ToggleDebugMode()
        {
            _debugMode = !_debugMode;
        }
    }
}
