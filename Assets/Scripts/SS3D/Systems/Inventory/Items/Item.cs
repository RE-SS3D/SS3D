using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using SS3D.Attributes;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR

#endif

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// An item describes what is held in a container.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequiredLayer("Items")]
    public class Item : InteractionSource, IInteractionTarget
    {
        #region Item
        [Header("Item settings")]

        [FormerlySerializedAs("Name")]
        [SerializeField] private string _name;

        [FormerlySerializedAs("Weight")]
        [SerializeField] private float _weight;

        [FormerlySerializedAs("Size")]
        [SerializeField] private Vector2Int _size;

        [FormerlySerializedAs("Traits")]
        [SerializeField] private List<Trait> _startingTraits;

        [SerializeField] private Rigidbody _rigidbody;

        private Sprite _sprite;

        [Tooltip("the item prefab, you can click on the item name and drag from Unity's file explorer")]
        public GameObject Prefab;

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

        [SyncVar]
        private Container _container;

        public string Name => _name;
        public ItemId ItemId { get; set; }
        public Vector2Int Size => _size;
        public ReadOnlyCollection<Trait> Traits => ((List<Trait>) _traits.Collection).AsReadOnly();

        public Container Container => _container;

        private bool _initialised = false;

        /// <summary>
        /// Initialise this item fields. Can only be called once.
        /// </summary>
        public void Init(string name, float weight, Vector2Int size,  List<Trait> traits)
        {
            if (_initialised)
            {
                Punpun.Error(this, "Item already initialised, returning");
                return;
            }
            _name = name ?? string.Empty;
            _weight = weight;
            _size = size;
            _traits.AddRange(traits);
            _initialised = true;
        }

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>

        public Sprite Sprite
        {
            get => InventorySprite();
            set => _sprite = value;
        }

        protected override void OnStart()
        {
            base.OnStart();

            foreach (Animator animator in GetComponents<Animator>())
            {
                animator.keepAnimatorControllerStateOnDisable = true;
            }

            // Clients don't need to calculate physics for rigidbodies as this is handled by the server
            if (_rigidbody != null && IsClientOnly)
            {
                _rigidbody.isKinematic = true;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _traits.AddRange(_startingTraits);
        }


        [ServerOrClient]
        private Sprite InventorySprite()
        {
            return _sprite == null ? GenerateNewIcon() : Sprite;
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        [Server]
        public void Delete()
        {
            SetContainer(null);

            if (GameObject != null)
            {
                ServerManager.Despawn(GameObject);
            }
        }

        /// <summary>
        /// Freezes the item, making it not move or collide
        /// </summary>
        [Server]
        public void Freeze()
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            var itemCollider = GetComponent<Collider>();
            if (itemCollider != null)
            {
                Punpun.Debug(this, "item {item} frozen", Logs.Generic, Name);
                itemCollider.enabled = false;
            }
        }

        /// <summary>
        /// Unfreezes the item, restoring normal functionality
        /// </summary>
        [Server]
        public void Unfreeze()
        {
            if (_rigidbody != null)
            {
                if (IsServer)
                    _rigidbody.isKinematic = false;
            }
            var itemCollider = GetComponent<Collider>();
            if (itemCollider != null)
            {
                itemCollider.enabled = true;
            }
        }

        /// <summary>
        /// Sets if this item is visible or not
        /// </summary>
        /// <param name="visible">Should the item be visible</param>
        [Server]
        public void SetVisibility(bool visible)
        {
            // TODO: Make this handle multiple renderers, with different states
            Renderer renderer = GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        /// <summary>
        /// Is this item visible in any way
        /// </summary>
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
        public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateSourceInteractions(targets, interactions);
            DropInteraction dropInteraction = new();

            interactions.Add(new InteractionEntry(null, dropInteraction));
        }

        /// <summary>
        /// Checks if the item is currently stored in a container
        /// </summary>
        /// <returns></returns>
        [ServerOrClient]
        public bool InContainer()
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
            foreach (var trait in _traits)
            {
                traits += trait.Name + " ";
            }
            return $"{Name}, size = {Size}, weight = {_weight}, traits = {traits}, container is {_container?.ContainerName}";
        }

        /// <summary>
        /// Checks if the item has an specific trait
        /// </summary>
        /// <param name="trait"></param>
        /// <returns></returns>
        [ServerOrClient]
        public bool HasTrait(Trait trait)
        {
            return _traits.Contains(trait);
        }

        /// <summary>
        /// Modify the container of this item, can pass null to make this item not depending on any container.
        /// </summary>
        [Server]
        public void SetContainer(Container newContainer)
        {
            if (_container == newContainer)
            {
                return;
            }

            if (_container != null && _container.ContainsItem(this))
            {
                Container.RemoveItem(this);
            }

            if (newContainer != null && !newContainer.ContainsItem(this))
            {
                newContainer.AddItem(this);
            }

            _container = newContainer;
        }

        // TODO: Improve this
        // we have this to generate icons at start, I do not know how bad it is for performance
        // if you know anything about it, tell us
        [ServerOrClient]
        public Sprite GenerateNewIcon()
        {
            RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
            RuntimePreviewGenerator.OrthographicMode = true;

            try
            {
                Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(this.transform,
            Shader.Find("Legacy Shaders/Diffuse"), null, 128, 128, true, true);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
                sprite.name = transform.name;
                return sprite;
            }
            catch (NullReferenceException)
            {
                Debug.LogError("Null reference exception, reverting to default sprite for item " + name + ".");
                return null;
            }

        }

        /// <summary>
        /// Check if size is correctly defined, and if not set it as (1,1).
        /// </summary>
        [ServerOrClient]
        public void ValidateSize()
        {
            // Items can't have no size
            if (Size.x <= 0)
            {
                _size = new Vector2Int(1, Size.y);
                Punpun.Warning(this, "item size in x lesser or equal zero, reverting it to 1");
            }

            if (Size.y <= 0)
            {
                _size = new Vector2Int(Size.x, 1);
                Punpun.Warning(this, "item size in y lesser or equal zero, reverting it to 1");
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
                Punpun.Warning(this, "item already contains trait {trait}", Logs.Generic, trait.Name);
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
