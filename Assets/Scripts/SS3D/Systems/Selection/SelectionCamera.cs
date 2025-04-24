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

        protected override void OnStart()
        {
            _system = SubSystems.Get<SelectionSubSystem>();
            _camera = GetComponent<Camera>();
            _playerCamera = transform.parent.GetComponent<Camera>();
            _camera.SetReplacementShader(_shader, "");

            GenerateRenderTexture();
            GenerateReadbackTexture();
            SubSystems.Get<InputSubSystem>().Inputs.Other.ToggleSelectionDebug.performed += ToggleDebugMode;
        }

        private void GenerateReadbackTexture()
        {
            _readbackTexture = new Texture2D(1, 1, _renderTexture.graphicsFormat, TextureCreationFlags.None);
        }

        private void GenerateRenderTexture()
        {
            if (_renderTexture != null) _renderTexture.Release();
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 0)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                autoGenerateMips = false,
                depth = 24
            };
            _camera.targetTexture = _renderTexture;
        }

        private void OnPreRender()
        {
            if (_renderTexture.width != Screen.width || _renderTexture.height != Screen.height)
            {
                GenerateRenderTexture();
            }
        }

        private void OnPostRender()
        {
            Color32 col;
            Vector3 pos = Input.mousePosition;

            // If mouse position is out of bounds, default to black (i.e. no colour)
            if (pos.x < 0 || pos.x >= Screen.width || pos.y < 0 || pos.y >= Screen.height)
            {
                col = Color.black;
            }
            else
            {
                _readbackTexture.ReadPixels(new Rect(pos.x, Screen.height-pos.y-1, 1, 1), 0, 0, false);
                col = _readbackTexture.GetPixel(0, 0);
            }

            _system.UpdateColourFromCamera(col);
        }

        protected override void OnDestroyed()
        {
            _renderTexture.Release();
            SubSystems.Get<InputSubSystem>().Inputs.Other.ToggleSelectionDebug.performed -= ToggleDebugMode;
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