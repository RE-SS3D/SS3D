
using UnityEngine;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory.UI;


namespace SS3D.Engine.Inventory
{

    /// <summary>
    /// if is normal, has an UI, if Pile or Hidden, has no UI.
    /// Visible script only attached on container with an UI. 
    /// Warning, Pile container can have an UI when there is multiple containers on the same game object, in the context menu. 
    /// </summary>
    public enum ContainerType
    {
        Normal,
        Pile,
        Hidden
    }

    /// <summary>
    /// ContainerDescriptor manages every aspect of a container attached to a gameObject.
    /// It's purpose is to centralize all relevant aspect of a container, it should be the only component one has to deal with when 
    /// adding containers to a game object.
    /// </summary>
    [ExecuteAlways]
    public class ContainerDescriptor : MonoBehaviour
    {
        // References toward all container related scripts.
        public AttachedContainer attachedContainer;
        public AttachedContainerGenerator attachedContainerGenerator;
        public ContainerSync containerSync;
        public ContainerInteractive containerInteractive; 
        public VisibleContainer visibleContainer;

        public ContainerUi containerUi;

        // Open interaction icon, visible when opening a container.
        public Sprite openIcon;
        // Take interaction icon, visible when taking something from a container.
        public Sprite takeIcon;
        // Store interaction icon, visible when storing something in a container.
        public Sprite storeIcon;
        // View interaction icon, visible when viewing a container.
        public Sprite viewIcon;

        /// <summary>
        /// The local position of attached items
        /// </summary>
        public Vector3 attachmentOffset = Vector3.zero;

        /// <summary> Name of the container. </summary>
        public string containerName = "container";

        /// <summary> If the container is openable, this defines if things can be stored in the container without opening it. </summary>
        public bool onlyStoreWhenOpen = false;

        /// <summary> Defines the size of the container, every item takes a defined place inside a container. </summary>
        public Vector2Int size = new Vector2Int(0,0);

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        public bool hideItems = true;

        /// <summary> If items should be attached as children. </summary>
        public bool attachItems = true;

        /// <summary> The initial filter of the container. </summary>
        public Filter startFilter;

        /// <summary> Used as a flag to create relevant components for the container only once, when the containerDescriptor is added. </summary>
        public bool initialized = false;

        /// <summary> max distance at which the container is visible if not hidden </summary>
        public float maxDistance = 5f;

        public ContainerType containerType = ContainerType.Normal;

        public bool isOpenable;

        public bool isInteractive;

        public void Start()
        {
            // If container interactions icon are not defined at start, load default icons.
            openIcon = openIcon == null ? Resources.Load<Sprite>("Interactions/door") : openIcon;
            takeIcon = takeIcon == null ? Resources.Load<Sprite>("Interactions/take") : takeIcon;
            storeIcon = storeIcon == null ? Resources.Load<Sprite>("Interactions/discard") : storeIcon;
            viewIcon = viewIcon == null ? Resources.Load<Sprite>("Interactions/container") : viewIcon;
        }
    }
}
