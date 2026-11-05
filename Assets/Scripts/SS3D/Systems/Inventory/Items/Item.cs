using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Coimbra;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Selection;
using SS3D.Utils;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using AssetDatabase = UnityEditor.AssetDatabase;
using UnityEditor;
#endif

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// An item describes what is held in a container.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(Selectable))]
    [RequiredLayer("Items")]
    public class Item : InteractionSource, IInteractionTarget, IWorldObjectAsset
    {
        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
        [Header("This field is filled automatically by the AssetData system.")]
#endif
        private ObjectAssetReference _asset;

        #region Item
        [Header("Item settings")]
        [FormerlySerializedAs("Name")]
        [SerializeField] private string _name;

        [FormerlySerializedAs("Weight")]
        [SerializeField] private float _weight;

        [Tooltip("Physical size tier, checked against a container's MaxSizeClass fit ceiling."), SerializeField]
        private SizeClass _sizeClass = SizeClass.Normal;

        [Tooltip("If greater than 1, identical items (same source asset) collapse into one stack up to this count."), SerializeField]
        private int _maxStackSize = 1;

        /// <summary>
        /// How many units this item instance currently represents. Only meaningful when MaxStackSize > 1.
        /// </summary>
        [SyncVar]
        private int _stackCount = 1;

        [FormerlySerializedAs("Traits")]
        [SerializeField] private List<Trait> _startingTraits;

        [SerializeField] private Rigidbody _rigidbody;

        /// <summary>
        /// How often a loose server-simulated item re-checks floor support to decide whether it
        /// should fall. Items rest for long stretches, so this does not need to run per frame.
        /// </summary>
        private const float SupportCheckInterval = 0.25f;

        private float _nextSupportCheckTime;

        private Sprite _sprite;

        [Header("Attachment settings")]

        [Tooltip("a point we use to know how the item should be oriented when held in a hand")]
        public Transform AttachmentPoint;

        [Tooltip("same point but for the left hand, in cases where it's needed")]
        public Transform AttachmentPointAlt;

        /// <summary>
        /// The list of characteristics this Item has
        /// </summary>
        [SyncObject]
        private readonly SyncList<Trait> _traits = new();

        /// <summary>
        /// Where the item is stored
        /// </summary>
        [SyncVar]
        private AttachedContainer _container;

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                {
                    return _name;
                }

                string objectName = gameObject.name;
                const string cloneSuffix = "(Clone)";
                if (objectName.EndsWith(cloneSuffix))
                {
                    objectName = objectName[..^cloneSuffix.Length].TrimEnd();
                }

                return objectName;
            }
        }

        /// <summary>
        /// This item's own weight. Does not include the recursive weight of anything stored inside it —
        /// see AttachedContainer.Weight for that (Documents/design/inventory-storage.md §2, §7).
        /// </summary>
        public float Weight => _weight;

        public SizeClass SizeClass => _sizeClass;

        public int MaxStackSize => _maxStackSize;

        public bool IsStackable => _maxStackSize > 1;

        public int StackCount => _stackCount;

        public ReadOnlyCollection<Trait> Traits => ((List<Trait>) _traits.Collection).AsReadOnly();

        /// <summary>
        /// Where the item is stored
        /// </summary>
        public AttachedContainer Container => _container;

        private bool _initialised = false;
        
        /// <summary>
        /// All colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        private Collider[] _nativeColliders;
        /// <summary>
        /// All colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        public Collider[] NativeColliders
        {
            get
            {
                if (_nativeColliders == null)
                {
                    _nativeColliders = GetNativeColliders();
                }
                return _nativeColliders;
            }
            set => _nativeColliders = value;
        }

        public ObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Log.Warning(this, "Field {fieldName} is being modified in runtime. This should not happen in normal conditions.", Logs.Generic, nameof(Asset));
                }
                _asset = value;
            }
        }

        public Item Prefab => Asset ? Assets.Get<Item>(Asset) : null;

        /// <summary>
        /// Authored pitch/roll from the item prefab (e.g. -90° X so long items lie on their side).
        /// </summary>
        public Quaternion WorldRestRotation
        {
            get
            {
                Item prefab = Prefab;
                if (prefab != null && prefab != this)
                {
                    return prefab.transform.localRotation;
                }

                return transform.localRotation;
            }
        }

        /// <summary>
        /// World rotation for floor spawn/drop: entity yaw composed with <see cref="WorldRestRotation"/>.
        /// </summary>
        public Quaternion GetWorldFacing(float yawDegrees) =>
            ComposeWorldFacing(WorldRestRotation, yawDegrees);

        /// <summary>
        /// Compose yaw with an authored rest rotation (for callers that only have the prefab).
        /// </summary>
        public static Quaternion ComposeWorldFacing(Quaternion restRotation, float yawDegrees) =>
            Quaternion.Euler(0f, yawDegrees, 0f) * restRotation;

        /// <summary>
        /// Initialise this item fields. Can only be called once.
        /// </summary>
        public void Init(string itemName, float weight,  List<Trait> traits)
        {
            if (_initialised)
            {
                Log.Error(this, "Item already initialised, returning");
                return;
            }
            _name = itemName ?? string.Empty;
            _weight = weight;
            _traits.AddRange(traits);
            _initialised = true;
        }

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>
        public Sprite ItemSprite
        {
            get => InventorySprite();
            set => _sprite = value;
        }
        
        protected override void OnStart()
        {
            base.OnStart();

            foreach (Animator animator in GetComponents<Animator>())
            {
                animator.keepAnimatorStateOnDisable = true;
            }

            // Clients don't need to calculate physics for rigidbodies as this is handled by the server
            if (_rigidbody != null && IsClientOnly)
            {
                _rigidbody.isKinematic = true;
            }

            _nativeColliders ??= GetNativeColliders();
        }

        /// <summary>
        /// Get all colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        private Collider[] GetNativeColliders()
        {
            List<Collider> collidersToExcept = new();
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>(true);
            foreach (Item item in containers.SelectMany(container => container.Items))
            {
                collidersToExcept.AddRange(item.GetComponentsInChildren<Collider>(true));
            }

            // includeInactive: clothing may keep a folded child inactive until presentation applies;
            // root colliders must still participate in Freeze/Unfreeze.
            return GetComponentsInChildren<Collider>(true).Except(collidersToExcept).ToArray();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _traits.AddRange(_startingTraits);
            if (_rigidbody != null) AtmosLooseBodies.Register(_rigidbody);
        }

        public override void OnStopServer()
        {
            if (_rigidbody != null) AtmosLooseBodies.Unregister(_rigidbody);
            base.OnStopServer();
        }

        public override void Update()
        {
            base.Update();

            if (!IsServer || _rigidbody == null || _rigidbody.isKinematic)
            {
                return;
            }

            float now = Time.time;
            if (now < _nextSupportCheckTime)
            {
                return;
            }

            _nextSupportCheckTime = now + SupportCheckInterval;
            ApplySupportGravity();
        }

        /// <summary>
        /// Loose items follow the same zero-g rule as ragdolls: gravity only over floor support.
        /// Unknown support (no map yet / AOI lag) leaves the current setting alone.
        /// </summary>
        public static bool? UseGravityForSupport(HumanoidSupportState support) => support switch
        {
            HumanoidSupportState.Supported => true,
            HumanoidSupportState.Unsupported => false,
            _ => null,
        };

        [Server]
        private void ApplySupportGravity()
        {
            bool? useGravity = UseGravityForSupport(HumanoidSpaceSupport.GetSupportAt(transform.position));
            if (!useGravity.HasValue || _rigidbody.useGravity == useGravity.Value)
            {
                return;
            }

            if (!useGravity.Value)
            {
                // Do not carry deck-settling fall speed into zero-g drift.
                Vector3 velocity = _rigidbody.linearVelocity;
                velocity.y = 0f;
                _rigidbody.linearVelocity = velocity;
            }

            _rigidbody.useGravity = useGravity.Value;
        }


        [ServerOrClient]
        private Sprite InventorySprite()
        {
            if (_sprite == null)
            {
                _sprite = GenerateIcon();
            }
            return _sprite;
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        [Server]
        public void Delete()
        {
            Container.RemoveItem(this);

            if (GameObject != null)
            {
                ServerManager.Despawn(GameObject);
            }
        }

        /// <summary>
        /// Freezes the item, making it not move or collide
        /// </summary>
        [ServerOrClient]
        public void Freeze()
        {
            if (_rigidbody != null)
            {
                // Clients keep items kinematic (server simulates). Setting velocity on a
                // kinematic body warns every SyncList add — only clear when physics is live.
                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }

                _rigidbody.isKinematic = true;
            }
            ToggleCollider(false);
        }

        /// <summary>
        /// Unfreezes the item, restoring normal functionality
        /// </summary>
        [ServerOrClient]
        public void Unfreeze()
        {
            if (_rigidbody != null)
            {
                if (IsServer)
                {
                    _rigidbody.isKinematic = false;
                    // Take effect on the first physics step, before the item can start falling
                    // through deep space; Update's throttled check keeps it current afterwards.
                    ApplySupportGravity();
                    _nextSupportCheckTime = Time.time + SupportCheckInterval;
                }
            }
            ToggleCollider(true);
        }
        
        /// <summary>
        /// Enable or disable all colliders related to the item. Does not touch any colliders that would belong to stored items (if there are any).
        /// TODO : might want to replace GetComponentsInChildren with a manual setup of the container list.
        /// </summary>
        [ServerOrClient]
        protected virtual void ToggleCollider(bool isEnable)
        {
            foreach (Collider collider in NativeColliders) 
            { 
                collider.enabled = isEnable;
            }
        }
        
        /// <summary>
        /// Removes any interaction outline shells (added by <see cref="SS3D.Systems.Interactions.InteractionOutlineView"/>)
        /// from a preview clone, so they can't leak into generated icons.
        /// Must DestroyImmediate: Coimbra Dispose → Destroy is end-of-frame, but the preview camera
        /// renders in this frame while hover shells are still enabled.
        /// </summary>
        private static void RemoveInteractionOutlines(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.name == SS3D.Systems.Interactions.InteractionOutlineView.OutlineObjectName)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    continue;
                }

                RemoveInteractionOutlines(child);
            }
        }

        /// <param name="visible">Should the item be visible</param>
        [ServerOrClient]
        public void SetVisibility(bool visible)
        {
            // Re-assert folded world form before enabling renderers so unequip/drop shows the pile,
            // not the body-shaped child (ClothingItemPresentation / ClothesDisplayer).
            if (visible && TryGetComponent(out ClothingItemPresentation clothingPresentation))
            {
                clothingPresentation.ApplyWorldForm();
            }

            // TODO: Make this handle multiple renderers, with different states
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer childRenderer in renderers)
            {
                // Interaction outline shells manage their own visibility (see InteractionOutlineView);
                // toggling them here would leave them stuck visible when the outline is meant to be hidden.
                if (SS3D.Systems.Interactions.InteractionOutlineView.IsOutlineRenderer(childRenderer))
                {
                    continue;
                }

                childRenderer.enabled = visible;
            }
        }

        public bool IsVisible()
        {
            // TODO: Make this handle multiple renderers
            Renderer component = GetComponent<Renderer>();
            return component != null && component.enabled;
        }

        public virtual IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupInteraction { Icon = null } };
        }

        // this creates the base interactions for an item, in this case, the drop interaction
        public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            base.CreateSourceInteractions(targets, interactions, context);
            interactions.Add(InteractionEntry.SourceOnly(new DropInteraction()));

            // Improvised melee for any held item without a dedicated weapon profile.
            if (TryGetComponent(out MeleeWeaponItemExtension _)
                || TryGetComponent(out RangedWeaponItemExtension _))
            {
                return;
            }

            var improvisedHit = new MeleeHitInteraction(MeleeWeaponProfile.Improvised);
            if (!improvisedHit.CanStartSwing(this))
            {
                return;
            }

            foreach (IInteractionTarget target in targets)
            {
                if (target is not IGameObjectProvider provider)
                {
                    continue;
                }

                Entity entity = provider.GameObject.GetComponentInParent<Entity>();
                if (entity == null || entity.GetComponentInChildren<HumanHealthController>() == null)
                {
                    continue;
                }

                interactions.Add(new InteractionEntry(target, improvisedHit));
            }
        }

        /// <summary>
        /// Checks if the item is currently stored in a container
        /// </summary>
        /// <returns></returns>
        [ServerOrClient]
        public bool IsInContainer()
        {
            return _container != null;
        }

        /// <summary>
        /// Describe this item properties.
        /// </summary>
        [ServerOrClient]
        public string Describe()
        {
            string traits = "";
            foreach (Trait trait in _traits)
            {
                traits += trait.Name + " ";
            }
            return $"{Name}, weight = {_weight}, traits = {traits}, container is {_container?.ContainerName}";
        }

        /// <summary>
        /// Checks if the item has a specific trait
        /// </summary>
        [ServerOrClient]
        public bool HasTrait(Trait trait)
        {
            if (trait == null)
            {
                return false;
            }

            // SyncList traits on clients are CreateInstance copies — match Name+Category, not refs.
            foreach (Trait owned in _traits)
            {
                if (owned == null)
                {
                    continue;
                }

                if (owned == trait || (owned.Name == trait.Name && owned.Category == trait.Category))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Modify the container of this item, can pass null to make this item not depending on any container.
        /// </summary>
        [Server]
        public void SetContainer(AttachedContainer newContainer)
        {
            _container = newContainer;
        }

        /// <summary>
        /// Whether this item and <paramref name="other"/> are the same stackable definition and could
        /// share one stack. Does not check remaining capacity — see AttachedContainer's merge logic.
        /// </summary>
        [ServerOrClient]
        public bool CanMergeWith(Item other)
        {
            return IsStackable
                && other != null
                && other.IsStackable
                && Asset != null
                && other.Asset != null
                && Asset.Equals(other.Asset);
        }

        [Server]
        public void SetStackCount(int count)
        {
            _stackCount = Mathf.Max(1, count);
        }

       

        /// <summary>
        /// Sprite for Main HUD / storage slots. Hands and world use the default (folded for clothing).
        /// Equipment-doll worn slots pass <paramref name="preferWornShape"/>.
        /// </summary>
        [ServerOrClient]
        public Sprite GetHudSprite(bool preferWornShape = false)
        {
            if (preferWornShape && TryGetComponent(out ClothingItemPresentation clothingPresentation))
            {
                Sprite worn = clothingPresentation.GetWornHudSprite(this);
                if (worn != null)
                {
                    return worn;
                }
            }

            return ItemSprite;
        }

        // Generate preview of the same object, but without stored items.
        [ServerOrClient]
        public Sprite GenerateIcon(bool useWornShapedForm = false)
        {
#if UNITY_SERVER
            // Icon generation renders a camera to produce a preview texture, which is unavailable and
            // unnecessary on a dedicated server (no shaders are included in the build for it to use).
            return null;
#else
            // Same for headless / -nographics clients (multiplayer harness): NullGfxDevice cannot
            // run RuntimePreviewGenerator without URP GraphicsBuffer/Blitter exceptions.
            if (UnityEngine.Application.isBatchMode
                || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return null;
            }

            Quaternion previousPreviewRotation = RuntimePreviewGenerator.PreviewRotation;
            RuntimePreviewGenerator.PreviewRotation = WorldRestRotation;

            // Find stored items
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>();
            // If stored items are found, temporarily set their parents to null,
            // so IconPreviewGenerator won't generate stored items
            Dictionary<Transform, Transform> storedItemsWithParents = new Dictionary<Transform, Transform>();
            Transform previewObject = null;
            Sprite icon = null;
            try
            {
                foreach (AttachedContainer attachedContainer in containers)
                {
                    IEnumerable<Item> storedItems = attachedContainer.Items;
                    foreach (Item item in storedItems)
                    {
                        Transform itemTransform = item.transform;
                        storedItemsWithParents.Add(itemTransform, itemTransform.parent);
                    }
                }
                foreach (Transform storedItem in storedItemsWithParents.Keys)
                {
                    storedItem.parent = null;
                }

                // Stay in a loaded scene until bake finishes. HideAndDontSave makes
                // scene.IsValid() false, so RuntimePreviewGenerator re-clones; ClothingItemPresentation.Awake
                // then ApplyWorldForm and a worn bake becomes the folded pile again.
                previewObject = Instantiate(transform, null, false);
                RemoveInteractionOutlines(previewObject);
                Item previewItem = previewObject.GetComponent<Item>();
                if (previewItem.TryGetComponent(out ClothingItemPresentation presentation))
                {
                    presentation.PrepareIconPreview(useWornShapedForm);
                }
                if (useWornShapedForm && presentation != null)
                {
                    // Bypass SetVisibility — that re-applies world (folded) form.
                    presentation.ApplyWornShapedForm();
                    SetChildRenderersEnabled(previewObject, true);
                }
                else
                {
                    previewItem.SetVisibility(true);
                }

                try
                {
                    // Bright full-toon ObjectIcon — not live half-toon world mats.
                    Texture2D texture = IconPreviewGenerator.Generate(previewObject, 128, 128);
                    if (texture == null)
                    {
                        Log.Warning(this, "Can't generate icon for " + name + ".");
                        icon = null;
                    }
                    else
                    {
                        icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f), 100);
                        icon.name = transform.name;
                    }
                }
                catch (NullReferenceException)
                {
                    Log.Warning(this, "Can't generate icon for " + name + ".");
                    icon = null;
                }
            }
            finally
            {
                RuntimePreviewGenerator.PreviewRotation = previousPreviewRotation;
                if (previewObject != null)
                {
                    previewObject.gameObject.Dispose(false);
                }
                foreach (KeyValuePair<Transform, Transform> storedItemWithParent in storedItemsWithParents)
                {
                    storedItemWithParent.Key.parent = storedItemWithParent.Value;
                }
            }

            return icon;
#endif
        }

        private static void SetChildRenderersEnabled(Transform root, bool visible)
        {
            // includeInactive: worn-shaped clothing children start disabled; preview must enable them.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer childRenderer in renderers)
            {
                if (SS3D.Systems.Interactions.InteractionOutlineView.IsOutlineRenderer(childRenderer))
                {
                    continue;
                }

                childRenderer.enabled = visible;
            }
        }

        /// <summary>
        /// Add a new trait to this and sync it
        /// </summary>
        [Server]
        public void AddTrait(Trait trait)
        {
            if (_traits.Contains(trait))
            {
                Log.Warning(this, "item already contains trait {trait}", Logs.Generic, trait.Name);
                return;
            }
            _traits.Add(trait);
        }

        /// <summary>
        /// Remove a trait from this item.
        /// </summary>
        [Server]
        public void RemoveTraits(Trait trait)
        {
             _traits.Remove(trait);
        }

        #endregion

        #region Editor
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }

            Mesh handGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Other/HoldGizmo.fbx", typeof(Mesh));

            // Don't even have to check without attachment
            if (AttachmentPoint != null)
            {
                Gizmos.color = new Color32(255, 120, 20, 170);
                Quaternion localRotation = AttachmentPoint.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                Vector3 parentPosition = AttachmentPoint.parent.position;
                Vector3 position = AttachmentPoint.localPosition;
                // Draw a wire mesh of the rotated model
                Vector3 rotatedPoint = RotatePointAround(parentPosition, position, eulerAngles);
                rotatedPoint += new Vector3(0, position.z, position.y);
                Gizmos.DrawWireMesh(handGuide, AttachmentPoint.position, localRotation);
            }

            // Same for the Left Hand
            if (AttachmentPointAlt != null)
            {
                Gizmos.color = new Color32(255, 120, 20, 170);
                Quaternion localRotation = AttachmentPointAlt.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                Vector3 parentPosition = AttachmentPointAlt.parent.position;
                Vector3 position = AttachmentPointAlt.localPosition;
                // Draw a wire mesh of the rotated model
                Vector3 rotatedPoint = RotatePointAround(parentPosition, position, eulerAngles);
                rotatedPoint += new Vector3(0, position.z, position.y);
                Gizmos.DrawWireMesh(handGuide, AttachmentPointAlt.position, localRotation);
            }
        }

        private static Vector3 RotatePointAround(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot);
        }
#endif
        #endregion
    }
}
