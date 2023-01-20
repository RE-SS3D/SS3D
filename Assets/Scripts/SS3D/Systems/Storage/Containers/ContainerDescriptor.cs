using FishNet;
using FishNet.Connection;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.UI;
using SS3D.Systems.Traits;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace SS3D.Systems.Storage.Containers
{
    /// <summary>
    /// ContainerDescriptor manages every aspect of a container attached to a gameObject.
    /// It's purpose is to centralize all relevant aspect of a container, it should be the only component one has to deal with when
    /// adding containers to a game object.
    ///
    /// Warning: Many attributes should be private instead of public. They are currently public because ContainerDescriptorEditor
    /// needs to access them directly, not through accessors or properties.
    ///
    /// ContainerDescriptorEditor should be declared as friend of ContainerDescriptor and most attributes should be private.
    /// </summary>
    public class ContainerDescriptor : MonoBehaviour
    {
        public bool AutomaticContainerSetUp = false;
        // References toward all container related scripts.
        public AttachedContainer AttachedContainer;
        public Container Container;
        public ContainerInteractive ContainerInteractive;
        public ContainerItemDisplay ContainerItemDisplay;

        // reference towards the container UI linked to this container.
        [Tooltip("Reference towards the container UI linked to this container. Leave empty before run ! ")]
        public ContainerUi ContainerUi;

        [Tooltip("Open interaction icon, visible when opening a container.")]
        public Sprite OpenIcon;
        [Tooltip("Take interaction icon, visible when taking something from a container.")]
        public Sprite TakeIcon;
        [Tooltip("Store interaction icon, visible when storing something in a container.")]
        public Sprite StoreIcon;
        [Tooltip("View interaction icon, visible when viewing a container.")]
        public Sprite ViewIcon;

        [Tooltip("The local position of attached items.")]
        public Vector3 AttachmentOffset = Vector3.zero;
        [Tooltip("Name of the container.")]
        public string ContainerName = "container";
        [Tooltip("If the container is openable, this defines if things can be stored in the container without opening it.")]
        public bool OnlyStoreWhenOpen;
        [Tooltip("When the container UI is opened, if set true, the animation on the object is triggered.")]
        public bool OpenWhenContainerViewed;
        [Tooltip("Defines the size of the container, every item takes a defined place inside a container.")]
        public Vector2Int Size = new(0,0);

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        [Tooltip("Set visibility of items in container.")]
        public bool HideItems = true;
        [Tooltip("If items should be attached as children of the container's game object.")]
        public bool AttachItems = true;

        // Initialized should not be displayed, it's only useful for setting up the container in editor.
        [HideInInspector]
        public bool Initialized;
        [Tooltip("Max distance at which the container is visible if not hidden.")]
        public float MaxDistance = 5f;
        [Tooltip("If the container can be opened/closed, in the sense of having a close/open animation.")]
        public bool IsOpenable;
        [Tooltip("If the container should have the container's default interactions setting script.")]
        public bool IsInteractive;
        [Tooltip("If stuff inside the container can be seen using an UI.")]
        public bool HasUi;
        [Tooltip("If true, interactions in containerInteractive are ignored, instead, a script on the container's game object should implement IInteractionTarget.")]
        public bool HasCustomInteraction;
        [Tooltip("If the container renders items in custom position on the container.")]
        public bool HasCustomDisplay;
        [Tooltip(" The list of transforms defining where the items are displayed.")]
        public Transform[] Displays;
        [Tooltip(" The number of custom displays.")]
        public int NumberDisplay;
        [Tooltip("The filter on the container.")]
        public Filter StartFilter;
        [FormerlySerializedAs("ContainerType")] [Tooltip("Container type mostly allow to discriminate between diffent containers on a single prefab.")]
        public ContainerType Type;
        /// <summary>
        /// need some bool to override automatic setup and go manual instead. nat 01/10/23
        /// </summary>
        public void Awake()
        {
            Assert.IsNotNull(AttachedContainer);

            // If container interactions icon are not defined at start, load default icons.
            OpenIcon = AssetData.Get(InteractionIcons.Openbox);
            TakeIcon = AssetData.Get(InteractionIcons.Take);
            StoreIcon = AssetData.Get(InteractionIcons.Take);
            ViewIcon = AssetData.Get(InteractionIcons.Openbox);
        }
    }
}
