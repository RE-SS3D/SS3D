using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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
    public class ItemActor : InteractionSource, IInteractionTarget
    {
        #region ItemActor
        [Header("Item settings")]

        [FormerlySerializedAs("Name")]
        [SerializeField] private string _startingName;

        [FormerlySerializedAs("Weight")]
        [SerializeField] private float _startingWeight;

        [FormerlySerializedAs("Size")]
        [SerializeField] private Vector2Int _startingSize;

        [FormerlySerializedAs("Traits")]
        [SerializeField] private List<Trait> _startingTraits;

        [SerializeField] private Rigidbody _rigidbody;

        [Tooltip("the item prefab, you can click on the item name and drag from Unity's file explorer")]
        public GameObject Prefab;

        [Header("Attachment settings")]

        [Tooltip("a point we use to know how the item should be oriented when held in a hand")]
        public Transform AttachmentPoint;

        [Tooltip("same point but for the left hand, in cases where it's needed")]
        public Transform AttachmentPointAlt;

        private Item item;

        public Item GetItem => item ;

        public string Name => item.Name;
        public ItemId ItemId { get; set; }
        public Vector2Int Size => item.Size;

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>
        private Sprite _sprite;


        /// <summary>
        /// The list of characteristics this Item has
        /// </summary>
        private readonly List<Trait> _traits = new();

        public ReadOnlyCollection<Trait> Traits => _traits.AsReadOnly();

        public Sprite Sprite
        {
            get => _sprite;
            set => _sprite = value;
        }

        public Sprite InventorySprite
        {
            get
            {
                if (Sprite == null)
                {
                    GenerateNewIcon();
                }

                return Sprite;
            }
        }

        /// <summary>
        /// The stack of this item, can be null
        /// </summary>
        // public Stackable Stack => stack ? stack : stack = GetComponent<Stackable>();
        /// <summary>
        /// The container this item is in
        /// </summary>

        public new void Awake()
        {
            base.Awake();
            _traits.AddRange(_startingTraits);
            item = new Item(this, _startingName, _startingWeight, _startingSize, (List<Trait>) _traits);

            // Add a warning if an item is not on the Items layer (layer 10).
            // Not really needed any more because of the RequiredLayer attribute.
            if (gameObject.layer != 10)
            {
                Punpun.Warning(this, "Item {item} is on {layer} layer. Should be on Items layer.",
                    Logs.Generic, item.Name, LayerMask.LayerToName(gameObject.layer));
            }
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

        public override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.K))
            {
                foreach (Trait trait in Traits)
                {
                    Punpun.Debug(this, $"Item {Name} has trait {trait.Name}");
                }
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log(item.Describe());
            }
        }

        /// <summary>
        /// Changes the item actor's item id
        /// </summary>
        /// <param name="id">AssetDatabase's ItemId</param>
        public void SetId(ItemId id)
        {
            ItemId = id;
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        public void Delete()
        {
            item.SetContainer(null);

            if (GameObject != null)
            {
                ServerManager.Despawn(GameObject);
            }
        }

        /// <summary>
        /// Freezes the item, making it not move or collide
        /// </summary>
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

        private new void OnDestroy()
        {
            item.SetContainer(null);
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
        public bool ISOnContainer()
        {
            return item.IsOnContainer();
        }

        /// <summary>
        /// Checks if the item has an specific trait
        /// </summary>
        /// <param name="trait"></param>
        /// <returns></returns>
        public bool HasTrait(Trait trait)
        {
            return item.HasTrait(trait);
        }

        // It could become an issue that only observers see the container updated...
        [ObserversRpc]
        private void RpcSetContainer(Container newContainer)
        {
            if (IsServer)
            {
                return;
            }

            item._container = newContainer;
        }

        // TODO: Improve this
        // we have this to generate icons at start, I do not know how bad it is for performance
        // if you know anything about it, tell us
        public void GenerateNewIcon()
        {
            RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
            RuntimePreviewGenerator.OrthographicMode = true;

            try
            {
                Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(this.transform,
            Shader.Find("Legacy Shaders/Diffuse"), null, 128, 128, true, true);
                Sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
                Sprite.name = transform.name;
            }
            catch (NullReferenceException)
            {
                Debug.LogError("Null reference exception, reverting to default sprite for item " + name + ".");
            }

        }
        #endregion

        #region Item
        public class Item
        {

            public readonly ItemActor Actor; 

            /// <summary>
            /// The item's name in the UI
            /// </summary>
            private string _name;

            /// <summary>
            /// The item's relative weight in kilograms.
            /// </summary>
            private float _weight;

            /// <summary>
            /// The amount of slots the item will take in a container
            /// </summary>
            private Vector2Int _size;

            /// <summary>
            /// The list of characteristics this Item has
            /// </summary>
            private readonly List<Trait> _traits;

            /// <summary>
            /// The container the item is currently stored on
            /// </summary>
            public Container _container;

            public string Name => _name;

            public float Weight => _weight;

            public Vector2Int Size => _size;

            public ReadOnlyCollection<Trait> Traits => _traits.AsReadOnly();

            public Item()
            {

            }

            public Item(string name, float weight, Vector2Int size, List<Trait> traits)
            {
                _name = name;
                _weight = weight;
                _size = size;
                _traits = new List<Trait>();
                _traits.AddRange(traits);
                ValidateItem();
            }

            public Item(ItemActor actor, string name, float weight, Vector2Int size, List<Trait> initialTraits)
            {
                Actor = actor;
                _name = name;
                _weight = weight;
                _size = size;
                _traits = initialTraits;
                ValidateItem();
            }

            public Container Container
            {
                get => _container;
            }

            public bool IsOnContainer()
            {
                return _container != null;
            }

            public bool HasTrait(Trait trait)
            {
                return _traits.Contains(trait);
            }

            /// <summary>
            /// Validates the Item's properties
            /// </summary>
            private void ValidateItem()
            {
                // Items can't have no size
                if (_size.x == 0)
                {
                    _size = new Vector2Int(1, _size.y);
                }

                if (_size.y == 0)
                {
                    _size = new Vector2Int(_size.x, 1);
                }
            }

            public void Delete()
            {
                throw new NotImplementedException();
            }

            [Server]
            public void SetContainer(Container newContainer)
            {
                if (Container == newContainer)
                {
                    return;
                }

                if (Container != null && Container.ContainsItem(this))
                {
                    Container.RemoveItem(this);
                }

                if (newContainer != null && !newContainer.ContainsItem(this))
                {
                    newContainer.AddItem(this);
                }

                _container = newContainer;

                if(Actor != null)
                    Actor.RpcSetContainer(newContainer);

            }

            public override bool Equals(object obj)
            {
                if(obj is Item item)
                {
                    return item.Actor == Actor;
                }
                return false;
            }

            public string Describe()
            {
                return $"{_name}, size = {_size}, weight = {_weight}, container is {Container?.ContainerName}";
            }
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
