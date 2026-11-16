using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Client-only orbital camera used while the map editor is open.
    /// Focus stays on the construction ground plane (y = 0). Middle-mouse orbit
    /// freezes placement picks for the duration so holograms do not slide with the cursor.
    /// </summary>
    public sealed class MapEditorSession
    {
        private const float DefaultPitch = 45f;
        private const float DefaultDistance = 20f;
        private const float MinPitch = 15f;
        private const float MaxPitch = 80f;
        private const float MinDistance = 5f;
        private const float MaxDistance = 80f;
        // Mouse.delta is pixels/frame — do not multiply by deltaTime.
        private const float OrbitDegreesPerPixel = 0.25f;
        private const float BasePanSpeed = 15f;
        private const float BaseZoomPerScroll = 0.25f;

        private Camera _camera;
        private Transform _previousParent;
        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private Vector3 _entryFocus;
        private bool _active;
        private bool _orbiting;

        private float _yaw;
        private float _pitch = DefaultPitch;
        private float _distance = DefaultDistance;
        private Vector3 _focus;

        private float _panSpeedMultiplier = 1f;
        private float _orbitSpeedMultiplier = 1f;
        private float _zoomSpeedMultiplier = 1f;

        public bool IsActive => _active;

        /// <summary>True while middle-mouse orbit is held (placement picks should freeze).</summary>
        public bool IsOrbiting => _orbiting;

        public void Enter(Camera camera, Vector3 entryPosition)
        {
            if (_active || camera == null)
                return;

            _camera = camera;
            _savedPosition = camera.transform.position;
            _savedRotation = camera.transform.rotation;
            _previousParent = camera.transform.parent;

            // Focus under the camera on the ground — never along a shallow look ray
            // (that lands hundreds of metres away and makes WASD feel wrong).
            _focus = new Vector3(entryPosition.x, 0f, entryPosition.z);
            Ray screenCenter = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            if (new Plane(Vector3.up, 0f).Raycast(screenCenter, out float hit) && hit > 0f)
                _focus = screenCenter.GetPoint(hit);

            _entryFocus = _focus;
            _yaw = camera.transform.eulerAngles.y;
            _pitch = DefaultPitch;
            _distance = DefaultDistance;

            camera.transform.SetParent(null);
            ApplyCamera();
            _active = true;
        }

        public void Exit()
        {
            if (!_active || _camera == null)
                return;

            EndOrbit();
            _camera.transform.SetParent(_previousParent);
            _camera.transform.SetPositionAndRotation(_savedPosition, _savedRotation);
            _active = false;
            _camera = null;
        }

        public void CenterOn(Vector3 position)
        {
            if (!_active || _camera == null) return;
            _focus = new Vector3(position.x, 0f, position.z);
            ApplyCamera();
        }

        public void ResetPosition()
        {
            _focus = _entryFocus;
            _distance = DefaultDistance;
            _pitch = DefaultPitch;
            ApplyCamera();
        }

        /// <summary>Nudges yaw by a fixed step — the camera dial's rotate-left/right buttons.</summary>
        public void RotateStep(float degrees)
        {
            if (!_active)
                return;

            _yaw += degrees;
            ApplyCamera();
        }

        /// <summary>Nudges distance by a fixed step — the camera dial's zoom-in/out buttons.</summary>
        public void ZoomStep(float amount)
        {
            if (!_active)
                return;

            _distance = Mathf.Clamp(_distance - amount, MinDistance, MaxDistance);
            ApplyCamera();
        }

        /// <summary>
        /// Applies UI camera sliders. Speeds are on a 1–10 scale where 5 ≈ 1x.
        /// </summary>
        public void SetSpeeds(float zoomSpeed, float rotationSpeed)
        {
            _zoomSpeedMultiplier = Mathf.Max(0.1f, zoomSpeed / 5f);
            _orbitSpeedMultiplier = Mathf.Max(0.1f, rotationSpeed / 5f);
            _panSpeedMultiplier = _orbitSpeedMultiplier;
        }

        public void Update(float deltaTime)
        {
            if (!_active || _camera == null)
                return;

            UpdateOrbit();
            UpdatePan(deltaTime);
            UpdateZoom();
            ApplyCamera();
        }

        private void UpdateOrbit()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            bool wantOrbit = mouse.middleButton.isPressed;
            if (wantOrbit && !_orbiting)
                BeginOrbit();
            else if (!wantOrbit && _orbiting)
                EndOrbit();

            if (!_orbiting)
                return;

            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * OrbitDegreesPerPixel * _orbitSpeedMultiplier;
            _pitch -= delta.y * OrbitDegreesPerPixel * _orbitSpeedMultiplier;
            _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
        }

        private void UpdatePan(float deltaTime)
        {
            // Session polls Keyboard directly (MapEditor context masks Movement). While a UITK
            // TextField holds InputTextEntryScope, WASD must not pan the camera.
            if (InputInterface.IsCapturingText)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // Ctrl/Cmd/Alt chords (Ctrl+S save, Alt replace, etc.) must not also pan.
            if (IsChordModifierPressed(keyboard))
                return;

            // Pan in yaw space only — never from camera.forward (near-vertical pitch
            // collapses the flattened forward and sends WASD in random directions).
            float yawRad = _yaw * Mathf.Deg2Rad;
            Vector3 forward = new(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            Vector3 right = new(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));

            float speed = BasePanSpeed * _panSpeedMultiplier * deltaTime;
            if (keyboard.wKey.isPressed)
                _focus += forward * speed;
            if (keyboard.sKey.isPressed)
                _focus -= forward * speed;
            if (keyboard.aKey.isPressed)
                _focus -= right * speed;
            if (keyboard.dKey.isPressed)
                _focus += right * speed;

            _focus.y = 0f;
        }

        private static bool IsChordModifierPressed(Keyboard keyboard) =>
            keyboard.ctrlKey.isPressed
            || keyboard.altKey.isPressed
            || keyboard.leftCommandKey.isPressed
            || keyboard.rightCommandKey.isPressed;

        private void UpdateZoom()
        {
            // Map editor polls devices directly (InputContext.MapEditor masks Camera/Zoom).
            // Wheel over UITK chrome (object library ScrollView, popovers) must not zoom —
            // Input System SuppressBinding cannot gate raw Mouse.current.scroll.
            if (InputInterface.IsPointerOverInterface())
                return;

            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Abs(scroll) <= 0.01f)
                return;

            _distance = Mathf.Clamp(
                _distance - scroll * BaseZoomPerScroll * _zoomSpeedMultiplier,
                MinDistance,
                MaxDistance);
        }

        private void BeginOrbit()
        {
            _orbiting = true;
        }

        private void EndOrbit()
        {
            _orbiting = false;
        }

        private void ApplyCamera()
        {
            // Pitch around X, yaw around Y; sit distance behind the focus along -forward.
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(
                _focus + rotation * new Vector3(0f, 0f, -_distance),
                rotation);
        }
    }
}
