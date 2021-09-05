
using UnityEngine;
using UnityEditor;
using SS3D.Content.Furniture.Storage;
using System.Collections.Generic;

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

    [ExecuteAlways]
    public class ContainerDescriptor : MonoBehaviour
    {
        // References toward all container related scripts.
        public AttachedContainer attachedContainer;
        public AttachedContainerGenerator attachedContainerGenerator;
        public ContainerSync containerSync;
        public OpenableContainer openableContainer;
        public StorageContainer storageContainer;
        public VisibleContainer visibleContainer;

        // Container interaction icons, visible when interacting with a container.
        [SerializeField] private Sprite openIcon;
        [SerializeField] private Sprite takeIcon;
        [SerializeField] private Sprite storeIcon;
        [SerializeField] private Sprite viewIcon;

        public Sprite OpenIcon 
        {
            get
            {
                if (openIcon == null) { return openIcon = Resources.Load<Sprite>("Interactions/door"); }
                else return openIcon;
            }
            set { SetProperty(ref value, "openIcon"); }
        }
        public Sprite TakeIcon
        {
            get
            {
                if (takeIcon == null) { return takeIcon = Resources.Load<Sprite>("Interactions/take"); }
                else return takeIcon;
            }
            set { SetProperty(ref value, "takeIcon"); }             
        }
        public Sprite StoreIcon
        {
            get
            {
                if (storeIcon == null) { return storeIcon = Resources.Load<Sprite>("Interactions/discard"); }
                else return storeIcon;
            }
            set { SetProperty(ref value, "storeIcon"); }   
        }
        public Sprite ViewIcon
        {
            get
            {
                if (viewIcon == null) { return viewIcon = Resources.Load<Sprite>("Interactions/container"); }
                else return viewIcon;
            }
            set { SetProperty(ref value, "viewIcon"); }
        }

        public void Start()
        {
            // If container interactions icon are not defined at start, load default icons.
            openIcon = openIcon == null ? Resources.Load<Sprite>("Interactions/door") : openIcon;
            takeIcon = takeIcon == null ? Resources.Load<Sprite>("Interactions/take") : takeIcon;
            storeIcon = storeIcon == null ? Resources.Load<Sprite>("Interactions/discard") : storeIcon;
            viewIcon = viewIcon == null ? Resources.Load<Sprite>("Interactions/container") : viewIcon;
        }

        /// <summary> Name of the container. </summary>
        [SerializeField] private string containerName = "container";
        public string ContainerName
        {
            get { return containerName; }
            set { SetProperty(ref value, "containerName"); }
        }

        /// <summary> If the container is openable, this defines if things can be stored in the container without opening it. </summary>
        [SerializeField] private bool onlyStoreWhenOpen = false;
        public bool OnlyStoreWhenOpen
        {
            get { return onlyStoreWhenOpen; }
            set { SetProperty(ref value, "onlyStoreWhenOpen"); }
        }

        /// <summary> Defines the size of the container, every item takes a defined place inside a container. </summary>
        [SerializeField]  private Vector2Int size = new Vector2Int(0,0);
        public Vector2Int Size
        {
            get { return size; }
            set { SetProperty(ref value, "size"); }
        }

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        [SerializeField] private bool hideItems = true;
        public bool HideItems
        {
            get { return hideItems; }
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("hideItems");
                if (ContainerType == ContainerType.Hidden)
                {
                    sp.boolValue = true; 
                }
                else
                {
                    sp.boolValue = value;
                }  
                so.ApplyModifiedProperties();
            }
        }

        /// <summary> If items should be attached as children. </summary>
        [SerializeField] private bool attachItems = true;
        public bool AttachItems
        {
            get { return attachItems; }
            set { SetProperty(ref value, "attachItems"); }
        }

        /// <summary> The initial filter of the container. </summary>
        [SerializeField] private Filter startFilter;
        public Filter StartFilter
        {
            get { return startFilter; }
            set { SetProperty(ref value, "startFilter"); }
        }

        /// <summary> Used as a flag to create relevant components for the container only once, when the containerDescriptor is added. </summary>
        [SerializeField] private bool initialized = false;
        public bool Initialized
        {
            get { return initialized; }
            private set { SetProperty(ref value, "initialized"); }
        }

        #if UNITY_EDITOR
        private void OnDestroy()
        {
             RemoveContainer();           
        }
#endif

        /// <summary> max distance at which the container is visible if not hidden </summary>
        [SerializeField] private float maxDistance = 5f;
        public float MaxDistance
        {
            get { return maxDistance; }
            set { SetProperty(ref value, "maxDistance"); }
        }


        [SerializeField] private ContainerType containerType = ContainerType.Normal;
        public ContainerType ContainerType
        {
            get { return containerType; }
            set
            {
                SetProperty(ref value, "containerType");
                // Normal containers are always visible.
                if (value == ContainerType.Normal && visibleContainer == null)
                {
                    AddVisible();
                }
                // Hidden containers are never visible. For pile containers, it's not clear yet as they could have an UI. It might change in the future.
                if (value == ContainerType.Hidden || value == ContainerType.Pile)
                {
                    RemoveVisible();
                }
            }
        }

        public bool IsOpenable
        {
            get { return openableContainer != null; }
            set
            {
                // Containers are either Openable or Storage, they can't be both.
                if (value && openableContainer == null)
                {
                    RemoveStorage();
                    AddOpenable();
                    // Openable containers can't be hidden.
                    if (ContainerType == ContainerType.Hidden) ContainerType = ContainerType.Normal;
                }
                else if (!value && openableContainer != null)
                {
                    RemoveOpenable();
                }
            }
        }
        public bool IsStorage
        {
            get { return storageContainer != null; }
            set
            {
                // Containers are either Openable or Storage, they can't be both.
                if (value && storageContainer == null)
                {
                    AddStorage();
                    RemoveOpenable();
                }
                else if (!value && storageContainer != null)
                {
                    RemoveStorage();
                }
            }
        }

        /// <summary>
        /// Creates a basic container by adding all necessary components to the game object.
        /// </summary>
        public void AddBase()
        {
            if (!initialized)
            {
                AddAttached();
                AddGenerator();
                AddStorage();
                AddVisible();
                AddSync();
                Initialized = true;
            } 
        }

        /// <summary>
        /// Remove all components linked to this containerDescriptor.
        /// </summary>
        private void RemoveContainer()
        {
            RemoveAttached();
            RemoveGenerator();
            RemoveSync();
            RemoveOpenable();
            RemoveStorage();
            RemoveVisible();
        }

        private void AddVisible()
        {
            visibleContainer = gameObject.AddComponent<VisibleContainer>();
            visibleContainer.containerDescriptor = this;
        }

        private void RemoveVisible()
        {
            DestroyImmediate(visibleContainer, true);
        }

        private void AddOpenable()
        {
            openableContainer = gameObject.AddComponent<OpenableContainer>();
            openableContainer.containerDescriptor = this;

            // Containers are either Openable or Storage, they can't be both.
            if (IsStorage && storageContainer != null)
            {
                DestroyImmediate(storageContainer, true);
            }
        }

        private void RemoveOpenable()
        {
            if(openableContainer != null)
            {
                DestroyImmediate(openableContainer, true);
            }  
        }

        private void AddStorage()
        {
            storageContainer = gameObject.AddComponent<StorageContainer>();
            storageContainer.containerDescriptor = this;

            // Containers are either Openable or Storage, they can't be both.
            if (IsOpenable && openableContainer != null)
            {
                DestroyImmediate(openableContainer, true);
            }
        }

        private void RemoveStorage()
        {
            if(storageContainer != null)
            {
                DestroyImmediate(storageContainer, true);
            }    
        }

        private void AddGenerator()
        {
            attachedContainerGenerator = gameObject.AddComponent<AttachedContainerGenerator>();
            attachedContainerGenerator.containerDescriptor = this;
        }

        private void RemoveGenerator()
        {
            DestroyImmediate(attachedContainerGenerator, true);
        }

        private void AddAttached()
        {
            attachedContainer = gameObject.AddComponent<AttachedContainer>();
            attachedContainer.containerDescriptor = this;
        }

        private void RemoveAttached()
        {
            if(attachedContainer != null)
            {
                DestroyImmediate(attachedContainer, true);
            }     
        }

        private void AddSync()
        {
            // There should be only one container sync script for any game object.
            containerSync = gameObject.GetComponent<ContainerSync>();
            if (containerSync == null)
            {
                containerSync = gameObject.AddComponent<ContainerSync>();
            }
        }

        private void RemoveSync()
        {
            if (gameObject.GetComponent<AttachedContainer>() == null)
            {
                DestroyImmediate(containerSync, true);
            }
        }

        /// <summary>
        /// This method is necessary to register change made through the custom editor, ContainerDescriptorEditor.
        /// It simply take care of setting any variables modified through the custom editor.
        /// </summary>
        /// <param name="value"> the value to attribute to the variable </param>
        /// <param name="propertyName"> the name of the variable to modify </param>
        private void SetProperty<T>(ref T value, string propertyName)
        {
            // Serialises this object, we don't need to update it next as we only write on SerializedProperty objects in this method, we don't read them.
            SerializedObject so = new SerializedObject(this);
            SerializedProperty sp = so.FindProperty(propertyName);

            if (value == null)
            {
                return;
            }

            if (value is string)
            {
                sp.stringValue = value as string;
            }
            else if (value is bool)
            {
                sp.boolValue = (bool)(object)value;
            }
            else if (value is float)
            {
                sp.floatValue = (float)(object)value;
            }
            else if (value is Vector2Int)
            {
                sp.vector2IntValue = (Vector2Int)(object)value;
            }
            else if (value is Object)
            {
                sp.objectReferenceValue = value as Object;
            }
            else if (value.GetType().IsEnum)
            {
                sp.enumValueIndex = (int)(object)value;
            }

            so.ApplyModifiedProperties();
        }

    }

}
