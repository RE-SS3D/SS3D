using System.Collections.Generic;
using Coimbra;
using FishNet.Object;
using FishNet.Observing;
using SS3D.Systems.Combat;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;
using Cloth = SS3D.Systems.Inventory.Containers.Cloth;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace SS3D.Systems.Entities.Character
{
    /// <summary>
    /// Local-only humanoid preview booth for UITK Character Creator / lobby.
    /// Own camera, light(s), and RenderTexture on an unused layer so station lighting
    /// never affects the preview. Runtime-created — no scene wiring.
    /// </summary>
    public sealed class CharacterPreviewBooth : MonoBehaviour
    {
        /// <summary>
        /// Unused user layer (index 23) reserved for character preview isolation. Not 22: that is
        /// <c>RuntimePreviewGenerator</c>'s item/tile icon layer, and sharing it put the booth dummy in item icons.
        /// </summary>
        public const int PreviewLayer = 23;

        /// <summary>Kept apart from <c>RuntimePreviewGenerator</c>'s icon position (-250, -250, -250).</summary>
        private static readonly Vector3 BoothOrigin = new(-250f, -250f, 250f);
        private const float BaseYawDegrees = 0f;
        /// <summary>Peaceful locomotion blend tree: ~0.3 = walk, 1.0 = run.</summary>
        private const float PreviewWalkSpeed = 0.3f;
        /// <summary>Portrait crop height as a fraction of the full-body frame.</summary>
        private const float PortraitViewportHeight = 0.2f;
        private const float PortraitHeadOffset = 0.08f;

        [SerializeField] private GameObject _humanPrefab;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Light _keyLight;
        [SerializeField] private Light _fillLight;
        [SerializeField] private Transform _spawnPoint;
        // Portrait RT sized for the CC sidebar (~360px wide, tall frame) at high DPI.
        // Render above display size so UITK downscales — edges stay sharper than 1:1.
        [SerializeField] private int _textureWidth = 1536;
        [SerializeField] private int _textureHeight = 2048;
        [SerializeField] private int _antiAliasing = 8;

        private GameObject _dummy;
        private RenderTexture _renderTexture;
        private int _angleIndex;
        private float[] _morphs;
        private bool _hasMorphs;

        public RenderTexture PreviewTexture => _renderTexture;

        public GameObject Dummy => _dummy;

        public void Initialize(GameObject humanPrefab)
        {
            if (humanPrefab != null)
            {
                _humanPrefab = humanPrefab;
            }

            MoveToWorldSpace();
            EnsureCamera();
            EnsureLights();
            EnsureRenderTexture();
            EnsureDummy();
            FrameCamera();
            ApplyAngleYaw();
            SetActive(true);
        }

        public void SetActive(bool active)
        {
            if (_previewCamera != null)
            {
                _previewCamera.enabled = active;
            }

            if (_keyLight != null)
            {
                _keyLight.enabled = active;
            }

            if (_fillLight != null)
            {
                _fillLight.enabled = active;
            }

            if (_dummy != null)
            {
                _dummy.SetActive(active);
            }
        }

        /// <summary>0 = Front, 1 = Side, 2 = Back.</summary>
        public void SetAngleIndex(int angleIndex)
        {
            _angleIndex = Mathf.Clamp(angleIndex, 0, 2);
            ApplyAngleYaw();
        }

        /// <summary>
        /// Applies Human FBX body shape keys + height scale to the preview dummy.
        /// Values are 0–1; jaw is bipolar (thin ↔ wide).
        /// </summary>
        public void ApplyBodyMorphs(
            float female,
            float breasts,
            float fat,
            float muscle,
            float jaw,
            float height)
        {
            if (_dummy == null)
            {
                return;
            }

            HumanoidMorphApplier.Apply(_dummy, female, breasts, fat, muscle, jaw, height);
            _morphs = new[] { female, breasts, fat, muscle, jaw, height };
            _hasMorphs = true;
            // Do not reframe — auto-fit would cancel height scale in the viewport.
        }

        /// <summary>
        /// Spawns / swaps the hair and beard prefabs on the preview dummy and tints them.
        /// Pass null prefabs to clear the slot (show no hair / beard).
        /// </summary>
        public void ApplyStyle(
            GameObject hairPrefab,
            GameObject beardPrefab,
            Color hairColor,
            Color beardColor)
        {
            if (_dummy == null)
            {
                return;
            }

            HumanoidStyleApplier.Apply(_dummy, hairPrefab, beardPrefab, hairColor, beardColor);

            // Hair prefabs are spawned after EnsureDummy, so their layer must be fixed up here.
            SetLayerRecursively(_dummy.transform, PreviewLayer);
        }

        public void ApplyFace(Color skin, Color eyes, GameObject eyebrows, Color browColor)
        {
            if (_dummy == null) return;
            HumanoidStyleApplier.ApplyFace(_dummy, skin, eyes, eyebrows, browColor);
            SetLayerRecursively(_dummy.transform, PreviewLayer);
        }

        /// <summary>
        /// Dresses the dummy in <paramref name="worn"/> (lobby effort Decision 14): each clothed body part takes the mesh
        /// of the first item that has one for it, and every other part is hidden, so an empty list strips the dummy.
        /// Applies meshes to the existing rig; never spawns networked clothing Items.
        /// </summary>
        public void ApplyOutfit(IReadOnlyList<Cloth> worn)
        {
            if (_dummy == null)
            {
                return;
            }

            foreach (ClothedBodyPart part in _dummy.GetComponentsInChildren<ClothedBodyPart>(true))
            {
                Cloth source = null;
                Mesh mesh = null;
                if (worn != null)
                {
                    foreach (Cloth cloth in worn)
                    {
                        if (cloth != null && cloth.TryGetClothMesh(part.Type, out mesh))
                        {
                            source = cloth;
                            break;
                        }
                    }
                }

                if (source == null || !part.TryGetComponent(out SkinnedMeshRenderer renderer))
                {
                    part.gameObject.SetActive(false);
                    continue;
                }

                renderer.sharedMesh = mesh;
                renderer.enabled = true;
                part.ApplyMaterials(renderer, source.WornMaterials);
                part.gameObject.SetActive(true);
            }

            ApplyOcclusion(worn);
            // A new sharedMesh starts with zero shape-key weights; keep it fitted to the body.
            if (_hasMorphs)
            {
                HumanoidMorphApplier.Apply(_dummy, _morphs[0], _morphs[1], _morphs[2], _morphs[3], _morphs[4], _morphs[5]);
            }

            SetLayerRecursively(_dummy.transform, PreviewLayer);
        }

        /// <summary>Opaque clothing hides the body renderers it names, as <c>ClothesDisplayer</c> does in game.</summary>
        private void ApplyOcclusion(IReadOnlyList<Cloth> worn)
        {
            foreach (Renderer renderer in _dummy.GetComponentsInChildren<Renderer>(true))
            {
                bool occluded = false;
                if (worn != null)
                {
                    foreach (Cloth cloth in worn)
                    {
                        if (cloth != null && cloth.OccludedBodyRenderers != null
                            && System.Array.IndexOf(cloth.OccludedBodyRenderers, renderer.name) >= 0)
                        {
                            occluded = true;
                            break;
                        }
                    }
                }

                renderer.forceRenderingOff = occluded;
            }
        }

        /// <summary>
        /// Copies a square head crop of the last rendered frame into <paramref name="target"/>.
        /// Only the Front angle gives a usable portrait, so other angles return false.
        /// </summary>
        public bool TryCapturePortrait(RenderTexture target) =>
            CaptureHeadCrop(target, PortraitViewportHeight, PortraitHeadOffset, false);

        /// <summary>
        /// Copies a square crop around the head into <paramref name="target"/>: <paramref name="viewportHeight"/> is
        /// the crop's height as a fraction of the frame, <paramref name="headOffset"/> lifts its centre above the head
        /// bone in metres. <paramref name="renderNow"/> renders the camera first (the editor's style thumbnail baker,
        /// which has no frame loop). Only the Front angle gives a usable crop, so other angles return false.
        /// </summary>
        public bool CaptureHeadCrop(RenderTexture target, float viewportHeight, float headOffset, bool renderNow)
        {
            if (target == null || _renderTexture == null || _previewCamera == null || !_previewCamera.enabled
                || _dummy == null || _angleIndex != 0)
            {
                return false;
            }

            Transform head = FindHeadBone(_dummy.transform);
            if (head == null)
            {
                return false;
            }

            if (renderNow)
            {
                _previewCamera.Render();
            }

            // Head bone sits at the skull base; aim slightly above it for the face.
            Vector3 viewport = _previewCamera.WorldToViewportPoint(head.position + (Vector3.up * headOffset));
            float height = viewportHeight;
            float width = height * _renderTexture.height / _renderTexture.width;
            Vector2 offset = new(
                Mathf.Clamp(viewport.x - (width * 0.5f), 0f, 1f - width),
                Mathf.Clamp(viewport.y - (height * 0.5f), 0f, 1f - height));
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(_renderTexture, target, new Vector2(width, height), offset);
                return true;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static Transform FindHeadBone(Transform root)
        {
            if (root.name.Equals("head", System.StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindHeadBone(root.GetChild(i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public void DisposeBooth()
        {
            SetActive(false);

            if (_dummy != null)
            {
                _dummy.Dispose(true);
                _dummy = null;
            }

            if (_renderTexture != null)
            {
                if (_previewCamera != null)
                {
                    _previewCamera.targetTexture = null;
                }

                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            gameObject.Dispose(true);
        }

        private void OnDestroy()
        {
            if (_dummy != null)
            {
                _dummy.Dispose(true);
                _dummy = null;
            }

            if (_renderTexture != null)
            {
                if (_previewCamera != null)
                {
                    _previewCamera.targetTexture = null;
                }

                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }

        private void MoveToWorldSpace()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }

            transform.position = BoothOrigin;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void EnsureCamera()
        {
            if (_previewCamera != null)
            {
                return;
            }

            GameObject cameraObject = new("CharacterPreviewCamera");
            cameraObject.transform.SetParent(transform, false);
            _previewCamera = cameraObject.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            _previewCamera.fieldOfView = 26f;
            _previewCamera.nearClipPlane = 0.05f;
            _previewCamera.farClipPlane = 40f;
            _previewCamera.depth = -100f;
            _previewCamera.cullingMask = 1 << PreviewLayer;
            _previewCamera.allowHDR = false;
            _previewCamera.allowMSAA = true;
            _previewCamera.enabled = false;

            ConfigureUrpCamera(_previewCamera);

            AudioListener listener = cameraObject.GetComponent<AudioListener>();
            if (listener != null)
            {
                Destroy(listener);
            }
        }

        private void EnsureLights()
        {
            if (_keyLight == null)
            {
                GameObject keyObject = new("CharacterPreviewKeyLight");
                keyObject.transform.SetParent(transform, false);
                keyObject.transform.localPosition = new Vector3(1.2f, 2.2f, -1.6f);
                keyObject.transform.localRotation = Quaternion.Euler(35f, -25f, 0f);
                _keyLight = keyObject.AddComponent<Light>();
                _keyLight.type = LightType.Directional;
                _keyLight.color = new Color(1f, 0.96f, 0.9f);
                _keyLight.intensity = 1.1f;
                _keyLight.cullingMask = 1 << PreviewLayer;
                _keyLight.shadows = LightShadows.None;
                SetLayerRecursively(keyObject.transform, PreviewLayer);
            }

            if (_fillLight == null)
            {
                GameObject fillObject = new("CharacterPreviewFillLight");
                fillObject.transform.SetParent(transform, false);
                fillObject.transform.localPosition = new Vector3(-1.4f, 1.4f, -0.8f);
                fillObject.transform.localRotation = Quaternion.Euler(15f, 40f, 0f);
                _fillLight = fillObject.AddComponent<Light>();
                _fillLight.type = LightType.Directional;
                _fillLight.color = new Color(0.7f, 0.78f, 0.9f);
                _fillLight.intensity = 0.35f;
                _fillLight.cullingMask = 1 << PreviewLayer;
                _fillLight.shadows = LightShadows.None;
                SetLayerRecursively(fillObject.transform, PreviewLayer);
            }
        }

        private static void ConfigureUrpCamera(Camera camera)
        {
#if UNITY_RENDER_PIPELINE_UNIVERSAL
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
            cameraData.renderShadows = false;
            cameraData.dithering = false;
            // SMAA stacks with RT MSAA and is visible in UITK even when pipeline MSAA is modest.
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
#endif
        }

        private void EnsureRenderTexture()
        {
            if (_renderTexture != null)
            {
                return;
            }

            // Unity RT MSAA only accepts 1 / 2 / 4 / 8.
            int samples = _antiAliasing switch
            {
                <= 1 => 1,
                2 => 2,
                <= 4 => 4,
                _ => 8,
            };

            _renderTexture = new RenderTexture(_textureWidth, _textureHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "CharacterPreviewRT",
                antiAliasing = samples,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                anisoLevel = 0,
            };
            _renderTexture.Create();
            _previewCamera.targetTexture = _renderTexture;
        }

        private void EnsureDummy()
        {
            if (_dummy != null || _humanPrefab == null)
            {
                return;
            }

            Transform parent = _spawnPoint != null ? _spawnPoint : transform;

            // Instantiate inactive so gameplay components never Awake: on a remote client the booth is
            // built before CameraSubSystem exists and HumanoidController.Setup throws.
            GameObject staging = new("CharacterPreviewStaging");
            staging.SetActive(false);
            staging.transform.SetParent(parent, false);
            _dummy = Instantiate(_humanPrefab, staging.transform);
            _dummy.name = "CharacterPreviewDummy";

            StripNetworking(_dummy);
            DisableGameplay(_dummy);
            SetLayerRecursively(_dummy.transform, PreviewLayer);

            _dummy.transform.SetParent(parent, false);
            _dummy.transform.localPosition = Vector3.zero;
            _dummy.transform.localRotation = Quaternion.Euler(0f, BaseYawDegrees, 0f);
            _dummy.transform.localScale = Vector3.one;
            staging.Dispose(true);

            // Animator parameters set while inactive are lost on activation.
            ConfigurePreviewLocomotion(_dummy, PreviewWalkSpeed, PreviewWalkSpeed);
        }

        private void ApplyAngleYaw()
        {
            if (_dummy == null)
            {
                return;
            }

            float yaw = BaseYawDegrees + (_angleIndex * 90f);
            _dummy.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void FrameCamera()
        {
            if (_previewCamera == null || _dummy == null)
            {
                return;
            }

            // Instantiated rig transforms still carry the prefab's authoring pose until
            // the Animator evaluates; fit the same posed geometry the camera will render.
            foreach (Animator animator in _dummy.GetComponentsInChildren<Animator>(true))
            {
                if (animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                    animator.Update(0f);
            }
            Bounds bounds = CalculateBounds(_dummy);
            float height = Mathf.Max(bounds.size.y, 0.75f);

            // True body center — upper-torso bias pushed the dummy down and cropped the feet.
            Vector3 lookAt = bounds.center;
            lookAt.y = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.5f);

            // Fit full height with a small margin; padding > 1 pulls the camera back.
            // Leave room for walking poses, shoes and the maximum height morph without
            // auto-fitting every edit (which would hide the effect of the Height slider).
            const float verticalPadding = 1.2f;
            float halfHeight = height * 0.5f * verticalPadding;
            float distance = halfHeight / Mathf.Tan(_previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            distance = Mathf.Clamp(distance, 1.1f, 8f);

            // Human's front faces +Z at yaw 0; the camera sits on +Z looking at it.
            Vector3 camPos = lookAt + new Vector3(0f, 0f, distance);
            _previewCamera.transform.position = camPos;
            _previewCamera.transform.rotation = Quaternion.LookRotation(lookAt - camPos, Vector3.up);
            _previewCamera.enabled = true;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            // Imported skinned bounds include animation safety margins and hidden parts;
            // fitting those makes the visible character tiny. Measure actual visible vertices
            // once when creating the booth, while keeping static gear in the fit as well.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new(root.transform.position, Vector3.one * 0.1f);
            bool initialized = false;
            Mesh bakedMesh = new();
            List<Vector3> vertices = new();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;

                if (renderer is SkinnedMeshRenderer skinned)
                {
                    // Empty equipment slots still have large authored renderer bounds.
                    if (skinned.sharedMesh == null) continue;
                    skinned.BakeMesh(bakedMesh);
                    bakedMesh.GetVertices(vertices);
                    foreach (Vector3 vertex in vertices)
                    {
                        Vector3 worldPoint = renderer.transform.TransformPoint(vertex);
                        if (!initialized)
                        {
                            bounds = new Bounds(worldPoint, Vector3.zero);
                            initialized = true;
                        }
                        else
                        {
                            bounds.Encapsulate(worldPoint);
                        }
                    }
                }
                else if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            Destroy(bakedMesh);
            return bounds;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private static void StripNetworking(GameObject root)
        {
            // Remove RequireComponent dependents before their providers. Prefab component
            // order is not dependency order, and the body bridge is not a NetworkBehaviour.
            DestroyAll<CombatInteractionNetwork>(root);
            DestroyAll<LocalSpeechEmitter>(root);
            DestroyAll<HumanoidCombatController>(root);
            DestroyAll<HumanoidBodyStateBridge>(root);
            DestroyAll<RagdollPart>(root);
            // Asserts its AttachedContainer (a NetworkBehaviour) in Awake, which still runs when disabled.
            DestroyAll<ContainerItemDisplay>(root);
            DestroyAll<NetworkObserver>(root);
            DestroyAll<NetworkBehaviour>(root);
            DestroyAll<NetworkObject>(root);
        }

        private static void DestroyAll<T>(GameObject root) where T : Object
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null)
                {
                    continue;
                }

                // Immediate: the dummy activates in the same frame, and a deferred Destroy would still Awake.
                DestroyImmediate(component);
            }
        }

        private static void DisableGameplay(GameObject root)
        {
            foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is Animator animator)
                {
                    animator.enabled = true;
                    animator.speed = 1f;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    continue;
                }

                // Leave only the Animator driving preview locomotion; strip everything else.
                behaviour.enabled = false;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody body in bodies)
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }

        private static void ConfigurePreviewLocomotion(GameObject root, float speed, float velZ)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null)
                {
                    continue;
                }

                animator.SetFloat(Animations.Humanoid.MovementSpeed, speed);
                animator.SetFloat(Animations.Humanoid.VelX, 0f);
                animator.SetFloat(Animations.Humanoid.VelZ, velZ);
                animator.SetFloat(Animations.Humanoid.Turn, 0f);
                animator.SetBool(Animations.Humanoid.Floating, false);
            }
        }
    }
}
