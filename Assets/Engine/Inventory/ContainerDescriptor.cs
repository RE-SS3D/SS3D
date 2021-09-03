
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

        [SerializeField] private string containerName = "container";
        public string ContainerName
        {
            get { return containerName; }
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("containerName");
                sp.stringValue = value;
                so.ApplyModifiedProperties();
            }
        }

        [SerializeField] private bool onlyStoreWhenOpen = false;
        public bool OnlyStoreWhenOpen
        {
            get { return onlyStoreWhenOpen; }
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("onlyStoreWhenOpen");
                sp.boolValue = value;
                so.ApplyModifiedProperties();
            }
        }

        [SerializeField]  private Vector2Int size = new Vector2Int(0,0);
        public Vector2Int Size
        {
            get { return size; }
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("size");
                sp.vector2IntValue = value;
                so.ApplyModifiedProperties();
            }
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
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("attachItems");
                sp.boolValue = value;
                so.ApplyModifiedProperties();
            }
        }

        [SerializeField] private Filter startFilter;
        public Filter StartFilter
        {
            get { return startFilter; }
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("startFilter");
                sp.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
        }

        [SerializeField] private bool initialized = false;
        public bool Initialized
        {
            get { return initialized; }
            private set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("initialized");
                sp.boolValue = true;
                so.ApplyModifiedProperties(); 
            }
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
            set
            {
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("maxDistance");
                sp.floatValue = value;
                so.ApplyModifiedProperties();
            }
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
                SerializedObject so = new SerializedObject(this);
                so.Update();
                SerializedProperty sp = so.FindProperty("containerType");
                sp.enumValueIndex = (int) value;
                so.ApplyModifiedProperties();

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
            DestroyImmediate(visibleContainer);
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

    }

}
