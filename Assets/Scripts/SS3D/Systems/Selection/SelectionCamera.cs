using UnityEngine;
using SS3D.Core.Behaviours;
using SS3D.Core;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Selection
{
    /// <summary>
    /// The purpose of the Selection Camera is simply to render the scene to an
    /// invisible RenderTexture. Each selectable (i.e. Examinable, Interactable)
    /// object is rendered in a different colour. Once rendered, the camera then
    /// reads back the colour of the pixel under the mouse, and sends that colour
    /// to the Selection System for further action.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SelectionCamera : Actor
    {
        /// <summary>
        /// The Selection Shader used to render the scene.
        /// </summary>
        [SerializeField] private Shader _shader;

        /// <summary>
        /// The camera used for rendering. It should be a child of the Main Camera.
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// The texture that this camera will render to.
        /// </summary>
        private RenderTexture _renderTexture;

        /// <summary>
        /// The texture which will return the colour of the pixel under the camera.
        /// </summary>
        private Texture2D _readbackTexture;

        /// <summary>
        /// Overarching System that performs all Selection-related processing.
        /// </summary>
        private SelectionSubSystem _system;

        /// <summary>
        /// Debug Mode allows the user to see the RenderTexture on screen, to facilitate debugging.
        /// </summary>
        private bool DebugMode = false;

        /// <summary>
        /// The Main Camera on the scene. Required here only for Debug Mode.
        /// </summary>
        private Camera _playerCamera;

        /// <summary>
        /// The input subsystem for subscribing to toggling debug mode
        /// </summary>
        private InputSubSystem _inputSystem;

        protected override void OnStart()
        {
            _system = SubSystems.Get<SelectionSubSystem>();
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _camera = GetComponent<Camera>();
            _playerCamera = transform.parent.GetComponent<Camera>();
            SyncFromPlayerCamera();

            EnsureRenderTextureSize();
            GenerateReadbackTexture();

            _inputSystem.Inputs.Other.ToggleSelectionDebug.performed += ToggleDebugMode;
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
        }

        protected override void OnDestroyed()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }
        }

        private void GenerateReadbackTexture()
        {
            _readbackTexture = new Texture2D(1, 1, _renderTexture.graphicsFormat, TextureCreationFlags.None);
        }

        private void EnsureRenderTextureSize()
        {
            int width = Mathf.Max(1, _playerCamera != null ? _playerCamera.pixelWidth : Screen.width);
            int height = Mathf.Max(1, _playerCamera != null ? _playerCamera.pixelHeight : Screen.height);

            if (_renderTexture != null && _renderTexture.width == width && _renderTexture.height == height)
            {
                return;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }

            _renderTexture = new RenderTexture(width, height, 0)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                autoGenerateMips = false,
                depth = 24
            };
            _camera.targetTexture = _renderTexture;
        }

        /// <summary>
        /// Keep projection in sync with the player camera so pick pixels line up with what is on screen.
        /// </summary>
        private void SyncFromPlayerCamera()
        {
            if (_playerCamera == null)
            {
                return;
            }

            _camera.CopyFrom(_playerCamera);
            _camera.targetTexture = _renderTexture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.SetReplacementShader(_shader, "");
        }

        private void OnPreRender()
        {
            SyncFromPlayerCamera();
            EnsureRenderTextureSize();
        }

        private void OnPostRender()
        {
            Color32 col = Color.black;

            if (_playerCamera != null
                && _renderTexture != null
                && _readbackTexture != null
                && TryGetMousePixel(out int x, out int y))
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

        /// <summary>
        /// Uses the selection shader to render directly to screen.
        /// To be removed from production code.
        /// </summary>
        public void ToggleDebugMode(InputAction.CallbackContext callbackContext)
        {
            if (DebugMode)
            {
                _playerCamera.ResetReplacementShader();
                DebugMode = false;
            }
            else
            {
                _playerCamera.SetReplacementShader(_shader, "");
                DebugMode = true;
            }
        }
    }
}
