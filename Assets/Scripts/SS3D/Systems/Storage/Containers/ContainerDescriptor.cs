using FishNet;
using FishNet.Connection;
using SS3D.Data;
using SS3D.Storage.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.UI;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Systems.Storage.Containers
{
    /// <summary>
    /// ContainerDescriptor manages every aspect of a container attached to a gameObject.
    /// It's purpose is to centralize all relevant aspect of a container, it should be the only component one has to deal with when 
    /// adding containers to a game object.
    /// 
    /// Warning: Many attributes should be private instead of public. They are currently public because ContainerDescriptorEditor
    /// needs to acces them directly, not through accessors or properties.
    ///
    /// ContainerDescriptorEditor should be declared as friend of ContainerDescriptor and most attributes should be private.
    /// </summary>
    public class ContainerDescriptor : MonoBehaviour
    {
        // References toward all container related scripts.
        public AttachedContainer AttachedContainer;
        public ContainerInteractive ContainerInteractive;
        public ContainerItemDisplay ContainerItemDisplay;
        // reference towards the container UI linked to this container.
        public ContainerUi ContainerUi;
        
        // Open interaction icon, visible when opening a container.
        public Sprite OpenIcon;
        // Take interaction icon, visible when taking something from a container.
        public Sprite TakeIcon;
        // Store interaction icon, visible when storing something in a container.
        public Sprite StoreIcon;
        // View interaction icon, visible when viewing a container.
        public Sprite ViewIcon;

        /// <summary>
        /// The local position of attached items
        /// </summary>
        public Vector3 AttachmentOffset = Vector3.zero;
        /// <summary> Name of the container. </summary>
        public string ContainerName = "container";
        /// <summary> If the container is openable, this defines if things can be stored in the container without opening it. </summary>
        public bool OnlyStoreWhenOpen;
        /// <summary> When the container UI is opened, if set true, the animation on the object is triggered </summary>
        public bool OpenWhenContainerViewed;
        /// <summary> Defines the size of the container, every item takes a defined place inside a container. </summary>
        public Vector2Int Size = new(0,0);
        
        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        [Tooltip("Set visibility of items in container.")]
        public bool HideItems = true;
        /// <summary> If items should be attached as children. </summary>
        public bool AttachItems = true;
        
        /// <summary> The initial filter of the container. Controls what can go in the container. </summary>
        public bool Initialized;
        /// <summary> max distance at which the container is visible if not hidden </summary>
        public float MaxDistance = 5f;
        public bool IsOpenable;
        /// <summary> If true, adds the containerInteractive script. Defines container interactions common to most containers. </summary>
        public bool IsInteractive;
        public bool HasUi;
        /// <summary> If true, interactions in containerInteractive are ignored, instead, a script should implement IInteractionTarget </summary>
        public bool HasCustomInteraction;
        /// <summary> If items in the container should be displayed at particular locations in the container</summary>
        public bool HasCustomDisplay;
        /// <summary> The list of transforms defining where the items are displayed.</summary>
        public Transform[] Displays;
        public int NumberDisplay;

        private float _lastObserverCheck;
        
        public void Awake()
        {
            // create a new container of Size size
            Assert.IsNotNull(AttachedContainer);

            // If container interactions icon are not defined at start, load default icons.
            OpenIcon = AssetData.Get(InteractionIcons.Open);
            TakeIcon = AssetData.Get(InteractionIcons.Take);
            StoreIcon = AssetData.Get(InteractionIcons.Take);
            ViewIcon = AssetData.Get(InteractionIcons.Open);
        }
    }
}
