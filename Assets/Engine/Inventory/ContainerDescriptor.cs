
using UnityEngine;
using UnityEditor;
using SS3D.Content.Furniture.Storage;
using System.Collections.Generic;

namespace SS3D.Engine.Inventory
{
    // if is normal, has an UI, if Pile or Hidden, has no UI.
    // Visible script only attached on container with an UI. 
    // Warning, Pile container can have an UI when there is multiple containers on the same game object, in the context menu. 
    public enum ContainerType
    {
        Normal,
        Pile,
        Hidden
    }

    [ExecuteAlways]
    public class ContainerDescriptor : MonoBehaviour
    {

        public AttachedContainer attachedContainer;
        public AttachedContainerGenerator attachedContainerGenerator;
        public ContainerSync containerSync;
        public OpenableContainer openableContainer;
        public StorageContainer storageContainer;
        public VisibleContainer visibleContainer;

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
            openIcon = openIcon == null ? Resources.Load<Sprite>("Interactions/door") : openIcon;
            takeIcon = takeIcon == null ? Resources.Load<Sprite>("Interactions/take") : takeIcon;
            storeIcon = storeIcon == null ? Resources.Load<Sprite>("Interactions/discard") : storeIcon;
            viewIcon = viewIcon == null ? Resources.Load<Sprite>("Interactions/container") : viewIcon;
        }

        [SerializeField] private string containerName = "container";
        public string ContainerName
        {
            get { return containerName; }
            set { SetProperty(ref value, "containerName"); }
        }

        [SerializeField] private bool onlyStoreWhenOpen = false;
        public bool OnlyStoreWhenOpen
        {
            get { return onlyStoreWhenOpen; }
            set { SetProperty(ref value, "onlyStoreWhenOpen"); }
        }

        [SerializeField]  private Vector2Int size = new Vector2Int(0,0);
        public Vector2Int Size
        {
            get { return size; }
            set { SetProperty(ref value, "size"); }
        }

        /// <summary>
        /// Set visibility of objects inside the container.
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

        /// <summary>
        /// If items should be attached as children
        /// </summary>
        [SerializeField] private bool attachItems = true;
        public bool AttachItems
        {
            get { return attachItems; }
            set { SetProperty(ref value, "attachItems"); }
        }

        [SerializeField] private Filter startFilter;
        public Filter StartFilter
        {
            get { return startFilter; }
            set { SetProperty(ref value, "startFilter"); }
        }

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

        // max distance at which the container is visible if not hidden
        [SerializeField] private float maxDistance = 5f;
        public float MaxDistance
        {
            get { return maxDistance; }
            set { SetProperty(ref value, "maxDistance"); }
        }

        /// <summary>
        /// The type of container, changes weather it has UI or not, or is hidden
        /// </summary>
        [SerializeField] private ContainerType containerType = ContainerType.Normal;
        public ContainerType ContainerType
        {
            get 
            {
                //if (containerType != null)
                    return containerType;
                //else return ContainerType.Normal;
            }
            set
            {
                SetProperty(ref value, "containerType");

                if (value == ContainerType.Normal && visibleContainer == null)
                {
                    AddVisible();
                }

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
                if (value && openableContainer == null)
                {
                    RemoveStorage();
                    AddOpenable();
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

        public void RemoveContainer()
        {
            RemoveAttached();
            RemoveGenerator();
            RemoveSync();
            RemoveOpenable();
            RemoveStorage();
            RemoveVisible();
        }

        public void AddVisible()
        {
            visibleContainer = gameObject.AddComponent<VisibleContainer>();
            visibleContainer.containerDescriptor = this;
        }

        public void RemoveVisible()
        {
            DestroyImmediate(visibleContainer, true);
        }

        public void AddOpenable()
        {
            openableContainer = gameObject.AddComponent<OpenableContainer>();
            openableContainer.containerDescriptor = this;
            if (IsStorage && storageContainer != null)
            {
                DestroyImmediate(storageContainer, true);
            }
        }

        public void RemoveOpenable()
        {
            if(openableContainer != null)
            {
                DestroyImmediate(openableContainer, true);
            }  
        }

        public void AddStorage()
        {
            storageContainer = gameObject.AddComponent<StorageContainer>();
            storageContainer.containerDescriptor = this;

            if (IsOpenable && openableContainer != null)
            {
                DestroyImmediate(openableContainer, true);
            }
        }

        public void RemoveStorage()
        {
            if(storageContainer != null)
            {
                DestroyImmediate(storageContainer, true);
            }    
        }

        public void AddGenerator()
        {
            attachedContainerGenerator = gameObject.AddComponent<AttachedContainerGenerator>();
            attachedContainerGenerator.containerDescriptor = this;
        }

        public void RemoveGenerator()
        {
            DestroyImmediate(attachedContainerGenerator, true);
        }

        public void AddAttached()
        {
            attachedContainer = gameObject.AddComponent<AttachedContainer>();
            attachedContainer.containerDescriptor = this;
        }

        public void RemoveAttached()
        {
            if(attachedContainer != null)
            {
                DestroyImmediate(attachedContainer, true);
            }     
        }

        public void AddSync()
        {
            containerSync = gameObject.GetComponent<ContainerSync>();
            if (containerSync == null)
            {
                containerSync = gameObject.AddComponent<ContainerSync>();
            }
        }

        public void RemoveSync()
        {
            if (gameObject.GetComponent<AttachedContainer>() == null)
            {
                DestroyImmediate(containerSync, true);
            }
        }

        private void SetProperty<T>(ref T value, string propertyName)
        {
            SerializedObject so = new SerializedObject(this);
            so.Update();
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
