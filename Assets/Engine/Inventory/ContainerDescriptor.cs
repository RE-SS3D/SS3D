
using UnityEngine;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory.UI;
using UnityEngine.Assertions;
using Mirror;
using SS3D.Content;

namespace SS3D.Engine.Inventory
{

    /// <summary>
    /// ContainerDescriptor manages every aspect of a container attached to a gameObject.
    /// It's purpose is to centralize all relevant aspect of a container, it should be the only component one has to deal with when 
    /// adding containers to a game object.
    /// Warning : Many attributes should be private instead of public. They are currently public because ContainerDescriptorEditor
    /// needs to acces them directly, not through accessors or properties. ContainerDescriptorEditor should be declared as friend of 
    /// ContainerDescriptor and most attributes should be private.
    /// </summary>
    public class ContainerDescriptor : MonoBehaviour
    {
        // References toward all container related scripts.
        public AttachedContainer attachedContainer;
        public ContainerSync containerSync;
        public ContainerInteractive containerInteractive;
        public ContainerItemDisplay containerItemDisplay;

        // reference towards the container UI linked to this container.
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

        /// <summary> When the container UI is opened, if set true, the animation on the object is triggered </summary>
        public bool openWhenContainerViewed = false;

        /// <summary> Defines the size of the container, every item takes a defined place inside a container. </summary>
        public Vector2Int size = new Vector2Int(0,0);

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        [Tooltip("Set visibility of items in container.")]
        public bool hideItems = true;

        /// <summary> If items should be attached as children. </summary>
        public bool attachItems = true;

        /// <summary> The initial filter of the container. Controls what can go in the container. </summary>
        public Filter startFilter;

        /// <summary> Used as a flag to create relevant components for the container only once, when the containerDescriptor is added.
        /// Used only by the Container Descriptor Editor</summary>
        public bool initialized = false;

        /// <summary> max distance at which the container is visible if not hidden </summary>
        public float maxDistance = 5f;

        public bool isOpenable;

        /// <summary> If true, adds the containerInteractive script. Defines container interactions common to most containers. </summary>
        public bool isInteractive;

        public bool hasUi;

        /// <summary> If true, interactions in containerInteractive are ignored, instead, a script should implement IInteractionTarget </summary>
        public bool hasCustomInteraction;

        /// <summary> If items in the container should be displayed at particular locations in the container</summary>
        public bool hasCustomDisplay;

        /// <summary> The list of transforms defining where the items are displayed.</summary>
        public Transform[] displays;

        public int numberDisplay;

        /// <summary>
        /// How often the observer list should be updated
        /// </summary>
        private float CheckObserversInterval = 1f;

        private float lastObserverCheck;
        
        public void Awake()
        {
            // create a new container of Size size
            Assert.IsNotNull(attachedContainer);
            attachedContainer.Container = new Container
            {
                Size = size
            };

            // add optional filters
            if (startFilter != null)
                attachedContainer.Container.Filters.Add(startFilter);

            // If container interactions icon are not defined at start, load default icons.
            openIcon = openIcon == null ? Resources.Load<Sprite>("Interactions/door") : openIcon;
            takeIcon = takeIcon == null ? Resources.Load<Sprite>("Interactions/take") : takeIcon;
            storeIcon = storeIcon == null ? Resources.Load<Sprite>("Interactions/discard") : storeIcon;
            viewIcon = viewIcon == null ? Resources.Load<Sprite>("Interactions/container") : viewIcon;
        }

        public void Update()
        {
                // this should be executed only for some kind of containers, not sure which one. It needs some conditions.
                UpdateObservers();
        }

        private void UpdateObservers()
        {
            if (lastObserverCheck + CheckObserversInterval < Time.time)
            {
                // Could probably be more efficient, it's currently checking every connection in game.
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    if (connection != null && connection.identity != null)
                    {
                        var creature = connection.identity.GetComponent<Entity>();
                        if (creature == null)
                        {
                            continue;
                        }
                        if (creature.CanSee(gameObject))
                        {
                            if (attachedContainer.Observers.Contains(creature))
                            {
                                continue;
                            }
                            attachedContainer.AddObserver(creature);
                        }
                        else 
                        {
                            attachedContainer.RemoveObserver(creature);
                        }
                    }
                }

                lastObserverCheck = Time.time;
            }
        }
    }
}
