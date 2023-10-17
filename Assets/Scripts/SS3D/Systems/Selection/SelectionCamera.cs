using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;
using SS3D.Core;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;

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
        private SelectionSystem _system;

        /// <summary>
        /// Debug Mode allows the user to see the RenderTexture on screen, to facilitate debugging.
        /// </summary>
        [SerializeField] private bool DebugMode = true;

        /// <summary>
        /// Image that Debug Mode draws to.
        /// </summary>
        [SerializeField] private RawImage _display;

        protected override void OnStart()
        {
            _system = Subsystems.Get<SelectionSystem>();
            _camera = GetComponent<Camera>();
            _camera.SetReplacementShader(_shader, "");

            GenerateRenderTexture();
            GenerateReadbackTexture();
            GenerateDebugDisplay();
        }

        private void GenerateDebugDisplay()
        {
            if (!DebugMode) return;
            _display.gameObject.SetActive(true);
            _display.texture = _renderTexture;
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
                _readbackTexture.ReadPixels(new Rect(pos.x, Screen.height - pos.y, 1, 1), 0, 0, false);
                //Color32 col = _readbackTexture.GetPixel((int)pos.x, (int)pos.y).ToString()}
                col = _readbackTexture.GetPixel(0, 0);
            }

            _system.UpdateColourFromCamera(col);
        }
    }
}