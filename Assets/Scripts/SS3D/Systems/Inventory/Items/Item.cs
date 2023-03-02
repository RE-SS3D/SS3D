using System;
using System.Collections.Generic;
using FishNet.Component.Transforming;
using FishNet.Object;
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
        [FormerlySerializedAs("_itemId")]
        [FormerlySerializedAs("_itemIdID")]
        [FormerlySerializedAs("ItemID")]
        [Header("Item settings")]
        [SerializeField]
        [HideInInspector]
        public Data.Enums.ItemId _itemId;

        [SerializeField] private string _name;

        [SerializeField] private Sprite _sprite;

        [SerializeField] private Rigidbody _rigidbody;

        /// <summary>
        /// The item's relative weight in kilograms.
        /// </summary>
        [SerializeField] private float _weight;

        [Tooltip("the item prefab, you can click on the item name and drag from Unity's file explorer")]
        public GameObject Prefab;
        [Header("Attachment settings")]
        [Tooltip("a point we use to know how the item should be oriented when held in a hand")]
        public Transform AttachmentPoint;
        [Tooltip("same point but for the left hand, in cases where it's needed")]
        public Transform AttachmentPointAlt;
        [Tooltip("The size of the item inside a container")]
        private Vector2Int _size;
        private Container _container;

        public Vector2Int Size => _size;

        public List<Trait> traits;

        public Sprite InventorySprite
        {
            get
            {
                if (_sprite == null)
                {
                    GenerateNewIcon();
                }

                return _sprite;
            }
        }

        // public Item()
        // {
        //     frozenItem = new FrozenItem(this);
        // }

        /// <summary>
        /// The stack of this item, can be null
        /// </summary>
        // public Stackable Stack => stack ? stack : stack = GetComponent<Stackable>();
        /// <summary>
        /// The container this item is in
        /// </summary>
        public Container Container
        {
            get => _container;
            set => SetContainer(value, false, false);
        }

        public void Awake()
        {
            _sprite = null;

            // Add a warning if an item is not on the Items layer (layer 10).
            // Not really needed any more because of the RequiredLayer attribute.
            if (gameObject.layer != 10)
            {;
                Punpun.Warning(this, "Item {ItemName} is on {LayerName)} layer. Should be on Items layer.", Logs.Generic, _name, LayerMask.LayerToName(gameObject.layer));
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

        /// <summary>
        /// Destroys this item
        /// </summary>
        public void Delete()
        {
            Container = null;

            if (GameObjectCache != null)
            {
                ServerManager.Despawn(GameObjectCache);
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

        private void OnDestroy()
        {
            _container = null;
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

        public bool InContainer()
        {
            return _container != null;
        }

        public bool HasTrait(Trait trait)
        {
            return traits.Contains(trait);
        }

        public bool HasTrait(string name)
        {
            var hash = Animator.StringToHash(name.ToUpper());
            foreach (Trait trait in traits)
            {
                if (trait.Hash == hash)
                    return true;
            }
            return false;
        }

        [Server]
        public void SetContainer(Container newContainer, bool alreadyAdded, bool alreadyRemoved)
        {
            if (_container == newContainer)
            {
                return;
            }

            if (_container != null)
            {
                _container.RemoveItem(this);
            }

            if (!alreadyAdded && newContainer != null)
            {
                newContainer.AddItem(this);
            }

            _container = newContainer;
            RpcSetContainer(newContainer);
        }

        [ObserversRpc]
        private void RpcSetContainer(Container newContainer)
        {
            if (IsServer)
            {
                return;
            }

            _container = newContainer;
        }

        /// <summary>
        /// Simply sets the container variable of this item, without doing anything
        /// <remarks>Make sure the item is only listed in the new container, or weird bugs will occur</remarks>
        /// </summary>
        public void SetContainerUnchecked(Container newContainer)
        {
            _container = newContainer;
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
                _sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
                _sprite.name = transform.name;
            }
            catch (NullReferenceException)
            {
                Punpun.Error(this, "Null reference exception, reverting to default sprite for item {ItemName}.", Logs.Generic, name) ;
            }

        }

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
    }
}
