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
        private bool _debugMode;

        protected override void OnStart()
        {
            _system = SubSystems.Get<SelectionSubSystem>();
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _camera = GetComponent<Camera>();
            _playerCamera = transform.parent.GetComponent<Camera>();

            // The child camera existed for Built-in replacement-shader rendering only.
            _camera.enabled = false;
            _camera.targetTexture = null;

            EnsureRenderTextureSize();
            GenerateReadbackTexture();
            _inputSystem.Inputs.Other.ToggleSelectionDebug.performed += ToggleDebugMode;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            _inputSystem = SubSystems.Get<InputSubSystem>();

            if (_inputSystem)
            {
                _inputSystem.Inputs.Other.ToggleSelectionDebug.performed += ToggleDebugMode;
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (_inputSystem)
            {
                _inputSystem.Inputs.Other.ToggleSelectionDebug.performed -= ToggleDebugMode;
            }

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

            EnsureRenderTextureSize();
            SelectionPickContext.SetRequest(new SelectionPickContext.Request
            {
                SourceCamera = _playerCamera,
                Target = _renderTexture,
                DebugView = _debugMode
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
            if (camera != _playerCamera || _system == null || _renderTexture == null || _readbackTexture == null)
            {
                return;
            }

            // Selection feeds examine/outlines/interaction hover. Clear it while over UI so world
            // targets do not "show through" registered UI Toolkit panels (Main HUD, MI, radial).
            Color32 col = Color.black;
            if (!InputInterface.IsPointerOverInterface() && TryGetMousePixel(out int x, out int y))
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _renderTexture;
                _readbackTexture.ReadPixels(new Rect(x, y, 1, 1), 0, 0, false);
                _readbackTexture.Apply(false, false);
                RenderTexture.active = previous;
                col = _readbackTexture.GetPixel(0, 0);
            }

            _system.UpdateColourFromCamera(col);
        }

        private bool TryGetMousePixel(out int x, out int y)
        {
            x = 0;
            y = 0;

            Vector3 screenPosition = Mouse.current != null
                ? (Vector3)Mouse.current.position.ReadValue()
                : Input.mousePosition;

            if (!_playerCamera.pixelRect.Contains(screenPosition))
            {
                return false;
            }

            Vector3 viewport = _playerCamera.ScreenToViewportPoint(screenPosition);
            x = Mathf.Clamp(Mathf.FloorToInt(viewport.x * _renderTexture.width), 0, _renderTexture.width - 1);
            y = Mathf.Clamp(Mathf.FloorToInt(viewport.y * _renderTexture.height), 0, _renderTexture.height - 1);
            return true;
        }

        public void ToggleDebugMode(InputAction.CallbackContext callbackContext)
        {
            _debugMode = !_debugMode;
        }
    }
}
