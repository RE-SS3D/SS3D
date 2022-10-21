using System.Collections.Generic;
using FishNet.Component.Transforming;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Storage.Containers;
using SS3D.Storage.Interactions;
using SS3D.Systems.Storage.Interactions;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace SS3D.Systems.Storage.Items
{
    /**
     * An item describes what is held in a container.
     */
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    public class Item : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        [Header("Item settings")]
        [SerializeField] private string _itemId;
        [SerializeField] private string _name;

        [Tooltip("The volume an item ocuppies in a container")]
        //private float _volume = 10f;

        // TODO: Make this private or just not show in the editor
        // the sprite used for containers UI
        [SerializeField] private Sprite _sprite;

        [Tooltip("the item prefab, you can click on the item name and drag from Unity's file explorer")]
        public GameObject Prefab;

        [Header("Attachment settings")]
        [Tooltip("a point we use to know how the item should be oriented when held in a hand")]
        public Transform AttachmentPoint;
        [Tooltip("same point but for the left hand, in cases where it's needed")]
        public Transform AttachmentPointAlt;
        // [Tooltip("the bulk of the item, how heavy it is")]
        // public BulkSize bulkSize = BulkSize.Medium;
        // [Tooltip("traits are attributes we use for stuff like 'is this item food', 'is this item a robot's part")]
        // public List<Trait> traits;
        [Tooltip("The size of the item inside a container")]
        private Vector2Int _size;
        // private Stackable stack;
        private Container _container;

        public Vector2Int Size => _size;

        public Sprite InventorySprite
        {
            get
            {
                if (_sprite == null)
                {
                    //GenerateNewIcon();
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

            // Add a warning if an item is not on the Item layer (layer 16).
            if (gameObject.layer != 16)
            {
                Debug.LogWarning("Item " + _name + " is on layer " + gameObject.layer);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            foreach (Animator animator in GetComponents<Animator>())
            {
                animator.keepAnimatorControllerStateOnDisable = true;
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

            // if (sprite == null && NetworkClient.active)
            // {
            //     GenerateNewIcon();
            // }
        }


        // TODO: Improve this
        // we have this to generate icons at start, I do not know how bad it is for performance
        // if you know anything about it, tell us
        // public void GenerateNewIcon()
        // {
        //     RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
        //     RuntimePreviewGenerator.OrthographicMode = true;
        //
        //     Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(this.transform, Shader.Find("Unlit/ItemPreview"), null, 128, 128, false);
        //     sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
        //     sprite.name = transform.name;
        // }

        // this creates the base interactions for an item, in this case, the drop interaction
        public override void GenerateInteractionsFromSource(IInteractionTarget[] targets,
            List<InteractionEntry> interactions)
        {
            base.GenerateInteractionsFromSource(targets, interactions);
            DropInteraction dropInteraction = new DropInteraction();
            interactions.Add(new InteractionEntry(null, dropInteraction));
        }

        /// <summary>
        /// Check if an item has at least a certain quantity
        /// </summary>
        /// <param name="amount">The amount to check</param>
        /// <returns>If the item has the required quantity</returns>
        /// <exception cref="ArgumentException">Thrown if the amount is less than one</exception>
        // public bool HasQuantity(int amount)
        // {
        //     if (amount < 1)
        //     {
        //         throw new ArgumentException("Amount must be at least 1", nameof(amount));
        //     }
        //     
        //     if (amount == 1)
        //     {
        //         return true;
        //     }
        //
        //     Stackable stackable = Stack;
        //     if (stackable == null)
        //     {
        //         return false;
        //     }
        //
        //     return stackable.amountInStack >= amount;
        // }

        /// <summary>
        /// Consumes a certain amount of this item
        /// </summary>
        /// <param name="amount">The amount to consume</param>
        // public void ConsumeQuantity(int amount)
        // {
        //     if (!HasQuantity(amount))
        //     {
        //         return;
        //     }
        //
        //     Stackable stackable = Stack;
        //     if (stackable != null)
        //     {
        //         stackable.amountInStack -= amount;
        //         if (stackable.amountInStack <= 0)
        //         {
        //             Destroy();
        //         }
        //     }
        //     else
        //     {
        //         Destroy();
        //     }
        // }

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

        }

        /// <summary>
        /// Unfreezes the item, restoring normal functionality
        /// </summary>
        public void Unfreeze()
        {

        }

        /// <summary>
        /// Sets if this item is visible or not
        /// </summary>
        /// <param name="visible">Should the item be visible</param>
        public void SetVisibility(bool visible)
        {
            // TODO: Make this handle multiple renderers, with different states
            Renderer renderer = GetComponent<Renderer>();
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

        void OnDestroy()
        {
            _container = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying ||
                UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }

            Mesh handGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Other/HoldGizmo.fbx", typeof(Mesh));

            // Don't even have to check without attachment
            if (AttachmentPoint == null)
            {
                return;
            }
            else
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
            if (AttachmentPointAlt == null)
            {
                return;
            }
            else
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

        private Vector3 RotatePointAround(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot);
        }

#endif
        public virtual IInteraction[] GetTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new PickupInteraction { InteractionIcon = _sprite } };
        }

        public bool InContainer()
        {
            return _container != null;
        }

        // public bool HasTrait(Trait trait)
        // {
        //     return traits.Contains(trait);
        // }
        //
        // public bool HasTrait(string name)
        // {
        //     var hash = Animator.StringToHash(name.ToUpper());
        //     foreach (Trait trait in traits)
        //     {
        //         if (trait.Hash == hash)
        //             return true;
        //     }
        //     return false;
        // }

        public void SetContainer(Container newContainer, bool alreadyAdded, bool alreadyRemoved)
        {
            if (_container == newContainer)
            {
                return;
            }

            _container?.RemoveItem(this);

            if (!alreadyAdded && newContainer != null)
            {
                newContainer.AddItem(this);
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
    }
}
