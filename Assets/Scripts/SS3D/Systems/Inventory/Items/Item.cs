using Coimbra;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Systems.Selection;
using SS3D.Traits;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using AssetDatabase = UnityEditor.AssetDatabase;
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
    public class Item : InteractionSource, IInteractionTarget, IWorldObjectAsset, ITraitsHolder
    {
        /// <summary>
        /// The list of characteristics this Item has
        /// </summary>
        [SyncObject]
        private readonly SyncList<Trait> _traits = new();

        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
        [Header("This field is filled automatically by the AssetData system.")]
#endif
        private WorldObjectAssetReference _asset;

        [Header("Item settings")]
        [SerializeField]
        private string _name;

        [SerializeField]
        private List<Trait> _startingTraits;

        [SerializeField]
        private Rigidbody _rigidbody;

        private Sprite _sprite;

        /// <summary>
        /// Where the item is stored
        /// </summary>
        [SyncVar]
        private AttachedContainer _container;

        private bool _initialised;

        /// <summary>
        /// All colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        private Collider[] _nativeColliders;

        public string Name => _name;

        public AbstractHoldable Holdable { get; private set; }

        public ReadOnlyCollection<Trait> Traits => ((List<Trait>)_traits.Collection).AsReadOnly();

        /// <summary>
        /// Where the item is stored
        /// </summary>
        public AttachedContainer Container => _container;

        public bool IsInContainer => _container != null;

        /// <summary>
        /// All colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        public Collider[] NativeColliders
        {
            get
            {
                return _nativeColliders ??= GetNativeColliders();
            }
            set => _nativeColliders = value;
        }

        public WorldObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Serilog.Log.Warning($"Field {nameof(Asset)} is being modified in runtime. This should not happen in normal conditions.");
                }

                _asset = value;
            }
        }

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>
        public Sprite ItemSprite
        {
            get => InventorySprite();
            set => _sprite = value;
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        /// <summary>
        /// Initialise this item fields. Can only be called once.
        /// </summary>
        public void Init(string itemName,  List<Trait> traits)
        {
            if (_initialised)
            {
                Log.Error(this, "Item already initialised, returning");
                return;
            }

            _name = itemName ?? string.Empty;
            _traits.AddRange(traits);
            _initialised = true;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _traits.AddRange(_startingTraits);
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        [Server]
        public void Delete()
        {
            Container.RemoveItem(this);

            if (GameObject)
            {
                ServerManager.Despawn(GameObject);
            }
        }

        /// <summary>
        /// Freezes the item, making it not move or collide
        /// </summary>
        [ServerOrClient]
        public void SetFreeze(bool isFrozen)
        {
            if (_rigidbody && IsServer)
            {
                _rigidbody.isKinematic = isFrozen;
            }

            ToggleCollider(!isFrozen);
        }

        /// <param name="visible">Should the item be visible</param>
        [ServerOrClient]
        public void SetVisibility(bool visible)
        {
            // TODO: Make this handle multiple renderers, with different states
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer childRenderer in renderers)
            {
                childRenderer.enabled = visible;
            }
        }

        public virtual IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupInteraction(0.2f, 0.2f) };
        }

        // this creates the base interactions for an item, in this case, the drop interaction
        public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries)
        {
            base.CreateSourceInteractions(targets, entries);
            DropInteraction dropInteraction = new();
            PlaceInteraction placeInteraction = new(0.2f, 0.2f);
            ThrowInteraction throwInteraction = new();

            entries.Add(new(null, dropInteraction));
            entries.Add(new(null, placeInteraction));
            entries.Add(new(null, throwInteraction));
        }

        /// <summary>
        /// Describe this item properties.
        /// </summary>
        [ServerOrClient]
        public string Describe()
        {
            string traits = string.Empty;
            foreach (Trait trait in _traits)
            {
                traits += trait.Name + " ";
            }

            return $"{Name}, traits = {traits}, container is {_container.ContainerName}";
        }

        /// <summary>
        /// Checks if the item has a specific trait
        /// </summary>
        [ServerOrClient]
        public bool HasTrait(Trait trait)
        {
            return _traits.Contains(trait);
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

        protected override void OnStart()
        {
            base.OnStart();

            Holdable = GetComponent<AbstractHoldable>();

            if (Holdable == null)
            {
                Holdable = gameObject.AddComponent<DefaultHoldable>();
            }

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
            Debug.Log("Start " + name);
        }

        /// <summary>
        /// Enable or disable all colliders related to the item. Does not touch any colliders that would belong to stored items (if there are any).
        /// TODO : might want to replace GetComponentsInChildren with a manual setup of the container list.
        /// </summary>
        [ServerOrClient]
        private void ToggleCollider(bool isEnable)
        {
            foreach (Collider collider in NativeColliders)
            {
                collider.enabled = isEnable;
            }
        }

        /// <summary>
        /// Get all colliders, related to the item, except of colliders, related to stored items
        /// </summary>
        private Collider[] GetNativeColliders()
        {
            List<Collider> collidersToExcept = new();
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>();
            foreach (Item item in containers.SelectMany(container => container.Items))
            {
                collidersToExcept.AddRange(item.GetComponentsInChildren<Collider>());
            }

            return GetComponentsInChildren<Collider>().Except(collidersToExcept).ToArray();
        }

        [ServerOrClient]
        private Sprite InventorySprite()
        {
            if (!_sprite)
            {
                _sprite = GenerateIcon();
            }

            return _sprite;
        }

        // Generate preview of the same object, but without stored items.
        [ServerOrClient]
        private Sprite GenerateIcon()
        {
            RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
            RuntimePreviewGenerator.OrthographicMode = true;

            // Find stored items
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>();

            // If stored items are found, temporarily set their parents to null,
            // so RuntimePreviewGenerator won't generate stored items
            Dictionary<Transform, Transform> storedItemsWithParents = new Dictionary<Transform, Transform>();
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

            Transform previewObject = Instantiate(transform, null, false);
            previewObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
            previewObject.GetComponent<Item>().SetVisibility(true);
            Sprite icon;
            try
            {
                Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(previewObject, Shader.Find("Legacy Shaders/Diffuse"), null, 128, 128);
                icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
                icon.name = transform.name;
            }
            catch (NullReferenceException)
            {
                Log.Warning(this, "Can't generate icon for " + name + ".");
                icon = null;
            }

            // Return stored items back to their parents
            previewObject.gameObject.Dispose(false);
            foreach (KeyValuePair<Transform, Transform> storedItemWithParent in storedItemsWithParents)
            {
                storedItemWithParent.Key.parent = storedItemWithParent.Value;
            }

            return icon;
        }
    }
}
