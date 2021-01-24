using System.Collections.Generic;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using UnityEditor;
using SS3D.Engine.Utilities;
using System;
using Mirror;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

namespace SS3D.Engine.Inventory
{
    /**
     * An item describes what is held in a container.
     */
    [DisallowMultipleComponent]
    public class Item : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        public string ItemId;
        public string Name;
        public float Volume = 10f;
        public Sprite sprite;
        public GameObject prefab;
        public Transform attachmentPoint;
        public BulkSize bulkSize = BulkSize.Medium;
        public List<Trait> traits;
        [Tooltip("The size of the item inside a container")]
        public Vector2Int Size;
        
        private Stackable stack;
        private Container container;
        private FrozenItem frozenItem;

        public Sprite InventorySprite
        {
            get
            {
                if (sprite == null)
                {
                    GenerateNewIcon();
                }

                return sprite;
            }
        }

        public Item()
        {
            frozenItem = new FrozenItem(this);
        }

        /// <summary>
        /// The stack of this item, can be null
        /// </summary>
        public Stackable Stack => stack ? stack : stack = GetComponent<Stackable>();
        /// <summary>
        /// The container this item is in
        /// </summary>
        public Container Container
        {
            get => container;
            set => SetContainer(value, false, false);
        }

        public void Awake()
        {
            sprite = null;
        }
        
        [ContextMenu("Create Icon")]
        public virtual void Start()
        {
            foreach (var animator in GetComponents<Animator>())
            {
                animator.keepAnimatorControllerStateOnDisable = true;
            }
            
            // Items can't have no size
            if (Size.x == 0)
            {
                Size = new Vector2Int(1, Size.y);
            }
            if (Size.y == 0)
            {
                Size = new Vector2Int(Size.x, 1);
            }
            
            if (sprite == null && NetworkClient.active)
            {
                GenerateNewIcon();
            }
        }

        public void GenerateNewIcon()
        {
            RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
            RuntimePreviewGenerator.OrthographicMode = true;

            Texture2D texture = RuntimePreviewGenerator.GenerateModelPreview(this.transform, 128, 128, false);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            sprite.name = transform.name;
        }
        
        public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateInteractions(targets, interactions);
            DropInteraction dropInteraction = new DropInteraction();
            interactions.Add(new InteractionEntry(null, dropInteraction));
        }

        /// <summary>
        /// Check if an item has at least a certain quantity
        /// </summary>
        /// <param name="amount">The amount to check</param>
        /// <returns>If the item has the required quantity</returns>
        /// <exception cref="ArgumentException">Thrown if the amount is less than one</exception>
        public bool HasQuantity(int amount)
        {
            if (amount < 1)
            {
                throw new ArgumentException("Amount must be at least 1", nameof(amount));
            }
            
            if (amount == 1)
            {
                return true;
            }

            Stackable stackable = Stack;
            if (stackable == null)
            {
                return false;
            }

            return stackable.amountInStack >= amount;
        }

        /// <summary>
        /// Consumes a certain amount of this item
        /// </summary>
        /// <param name="amount">The amount to consume</param>
        public void ConsumeQuantity(int amount)
        {
            if (!HasQuantity(amount))
            {
                return;
            }

            Stackable stackable = Stack;
            if (stackable != null)
            {
                stackable.amountInStack -= amount;
                if (stackable.amountInStack <= 0)
                {
                    Destroy();
                }
            }
            else
            {
                Destroy();
            }
        }

        /// <summary>
        /// Destroys this item
        /// </summary>
        public void Destroy()
        {
            Container = null;
            
            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }

        /// <summary>
        /// Freezes the item, making it not move or collide
        /// </summary>
        public void Freeze()
        {
            frozenItem.Freeze();
        }

        /// <summary>
        /// Unfreezes the item, restoring normal functionality
        /// </summary>
        public void Unfreeze()
        {
            frozenItem.Unfreeze();
        }

        /// <summary>
        /// Sets if this item is visible or not
        /// </summary>
        /// <param name="visible">Should the item be visible</param>
        public void SetVisibility(bool visible)
        {
            // TODO: Make this handle multiple renderers, with different states
            var renderer = GetComponent<Renderer>();
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
            var renderer = GetComponent<Renderer>();
            return renderer != null && renderer.enabled;
        }

        void OnDestroy()
        {
            container = null;
            frozenItem = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Don't even have to check without attachment
            if (attachmentPoint == null)
            {
                return;
            }

            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }


            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Quaternion localRotation = attachmentPoint.localRotation;
                Vector3 eulerAngles = localRotation.eulerAngles;
                Vector3 parentPosition = attachmentPoint.parent.position;
                Vector3 position = attachmentPoint.localPosition;
                // Draw a wire mesh of the rotated model
                Vector3 rotatedPoint = RotatePointAround(parentPosition, position, eulerAngles);
                rotatedPoint += new Vector3(0, position.z, position.y);
                Gizmos.DrawWireMesh(meshFilter.sharedMesh,
                    rotatedPoint, localRotation);
            }
        }

        private Vector3 RotatePointAround(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot);
        }

#endif
        public virtual IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupInteraction { icon = sprite } };
        }

        public bool InContainer()
        {
            return container != null;
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

        public void SetContainer(Container newContainer, bool alreadyAdded, bool alreadyRemoved)
        {
            if (container == newContainer)
            {
                return;
            }
            
            container?.RemoveItem(this);
            
            if (!alreadyAdded && newContainer != null)
            {
                newContainer.AddItem(this);
            }

            container = newContainer;
        }

        /// <summary>
        /// Simply sets the container variable of this item, without doing anything
        /// <remarks>Make sure the item is only listed in the new container, or weird bugs will occur</remarks>
        /// </summary>
        public void SetContainerUnchecked(Container newContainer)
        {
            container = newContainer;
        }
    }
}