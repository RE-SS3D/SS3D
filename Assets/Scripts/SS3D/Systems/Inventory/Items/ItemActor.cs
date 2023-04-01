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

        private Sprite _sprite;

        private Item _item;

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

        [SyncVar(Channel = Channel.Unreliable, OnChange = nameof(OnContainerSync))]
        private Container _container;

        public Item GetItem => _item;

        public string Name => _item.Name;
        public ItemId ItemId { get; set; }
        public Vector2Int Size => _item.Size;
        public ReadOnlyCollection<Trait> Traits => _item.Traits;

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>
        public Sprite Sprite
        {
            get => InventorySprite();
            set => _sprite = value;
        }

        public new void Awake()
        {
            base.Awake();
            _item = new Item(this, _startingName, _startingWeight, _startingSize, (List<Trait>)_traits.Collection, ref _container);
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



        public Sprite InventorySprite()
        {
            return _sprite == null ? GenerateNewIcon() : Sprite;
        }


        private void OnContainerSync(Container prev, Container next, bool asServer)
        {
            if (asServer || IsHost) return;
            _item.UnsafeSetContainer(next);
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        public void Delete()
        {
            _item.SetContainer(null);

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
            return _item.IsOnContainer();
        }

        /// <summary>
        /// Checks if the item has an specific trait
        /// </summary>
        /// <param name="trait"></param>
        /// <returns></returns>
        public bool HasTrait(Trait trait)
        {
            return _item.HasTrait(trait);
        }

        [Server]
        public void SetContainer(Container newContainer)
        {
            _item.SetContainer(newContainer);
        }

        // TODO: Improve this
        // we have this to generate icons at start, I do not know how bad it is for performance
        // if you know anything about it, tell us
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
        #endregion

        #region Item
        public class Item
        {

            public readonly ItemActor Actor;

            /// <summary>
            /// The item's name in the UI
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The item's relative weight in kilograms.
            /// </summary>
            public readonly float Weight;

            /// <summary>
            /// The amount of slots the item will take in a container
            /// </summary>
            public readonly Vector2Int Size;

            /// <summary>
            /// The list of characteristics this Item has
            /// </summary>
            private readonly List<Trait> _traits;

            /// <summary>
            /// The container the item is currently stored on
            /// </summary>
            private Container _container;

            public ReadOnlyCollection<Trait> Traits => _traits.AsReadOnly();

            private Item(string name, float weight, Vector2Int size)
            {
                Name = name;
                Weight = weight;
                Size = size;
                // Items can't have no size
                if (Size.x == 0)
                {
                    Size = new Vector2Int(1, Size.y);
                }

                if (Size.y == 0)
                {
                    Size = new Vector2Int(Size.x, 1);
                }
            }

            public Item(string name, float weight, Vector2Int size, List<Trait> traits) 
                : this(name, weight, size)
            {
                _traits = new List<Trait>();
                _traits.AddRange(traits);
            }

            public Item(ItemActor actor, string name, float weight, Vector2Int size, List<Trait> traits, ref Container container)
                : this(name, weight, size)
            {
                Actor = actor;
                _traits = traits;
                _container = container;
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

            public void AddTrait(Trait trait)
            {
                if(Actor != null) Actor._traits.Add(trait);
                else _traits.Add(trait);
            }

            public void RemoveTraits(Trait trait)
            {
                if (Actor != null) Actor._traits.Add(trait);
                else _traits.Remove(trait);
            }

            public void Delete()
            {
                if (Actor != null) Actor.Delete(); 
            }

            /// <summary>
            /// Do not use this unless you know what you're doing ! It will not sync stuff properly.
            /// </summary>
            /// <param name="newContainer"></param>
            public void UnsafeSetContainer(Container newContainer)
            {
                _container = newContainer;
            }


            /// <summary>
            /// Don't call this with client ! This should all be done server side.
            /// </summary>
            /// <param name="newContainer"></param>
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
                if(Actor != null) Actor._container = newContainer;
            }

            public override bool Equals(object obj)
            {
                if(obj is Item item)
                {
                    if (item.Actor != null)
                    {
                        // If there is an actor, the item will be the same if they have the same actor
                        return item.Actor == Actor;
                    }
                    // Otherwise fallback to checking the Item's name, useful for unit testing
                    return item.Name == Name;
                }
                return false;
            }

            public string Describe()
            {
                string traits = "";
                foreach(var trait in _traits)
                {
                    traits += trait.Name + " ";
                }
                return $"{Name}, size = {Size}, weight = {Weight}, traits = {traits}, container is {Container?.ContainerName}";
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
