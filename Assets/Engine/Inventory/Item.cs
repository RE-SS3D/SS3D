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
        [HideInInspector]public Container container;
        public Sprite sprite;
        public GameObject prefab;
        public Transform attachmentPoint;
        public BulkSize bulkSize = BulkSize.Medium;
        public List<Trait> traits;

        /// <summary>
        /// The stack of this item, can be null
        /// </summary>
        public Stackable Stack => stack ? stack : stack = GetComponent<Stackable>();
        private Stackable stack;

        [ContextMenu("Create Icon")]
        public void Start()
        {
            foreach (var animator in GetComponents<Animator>())
            {
                animator.keepAnimatorControllerStateOnDisable = true;
            }

            GenerateNewIcon();
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
            if (container != null)
            {
                container.RemoveItem(gameObject);
                container = null;
            }
            
            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (container != null)
            {
                container.RemoveItem(gameObject);
            }
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
    }
}