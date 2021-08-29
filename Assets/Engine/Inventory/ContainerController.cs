
using UnityEngine;
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

    /// <summary>
    /// A container attached to a gameobject
    /// </summary>
    public class ContainerController : MonoBehaviour
    {
        public List<ContainerDescriptor> ContainerDescriptors = new List<ContainerDescriptor>();

        public void AddBaseComponent()
        {
            ContainerDescriptor ContainerDescriptor = new ContainerDescriptor(gameObject);

            ContainerDescriptor.AddAttached();
            ContainerDescriptor.AddGenerator();
            ContainerDescriptor.AddStorage();
            ContainerDescriptor.AddVisible();
            ContainerDescriptor.AddSync();
          
            ContainerDescriptors.Add(ContainerDescriptor);
            Debug.Log("number of containers : " + ContainerDescriptors.Count.ToString());
        }

        public int FindIndexOf(AttachedContainer container)
        {
            int index = 0;
            foreach (ContainerDescriptor containerDescriptor in ContainerDescriptors)
            {
                if (container == containerDescriptor.AttachedContainer)
                {
                    return index;
                }
                index += 1;
            }
            Debug.Log("this container is not part of the list");
            return 0;
        }

        public void RemoveContainer(AttachedContainer container)
        {
            RemoveContainer(FindIndexOf(container));
        }

        public void RemoveContainer(int IndexOf)
        {
            DestroyImmediate(ContainerDescriptors[IndexOf].AttachedContainer, true);
            DestroyImmediate(ContainerDescriptors[IndexOf].AttachedContainerGenerator, true);
            if(ContainerDescriptors.Count == 1)
            {
                DestroyImmediate(ContainerDescriptors[IndexOf].ContainerSync, true);
            }
            DestroyImmediate(ContainerDescriptors[IndexOf].OpenableContainer, true);
            DestroyImmediate(ContainerDescriptors[IndexOf].StorageContainer, true);
            DestroyImmediate(ContainerDescriptors[IndexOf].VisibleContainer,true);
            ContainerDescriptors.RemoveAt(IndexOf);
        }

        public void RemoveAllContainers()
        {
            foreach(ContainerDescriptor containerDescriptor in ContainerDescriptors)
            {
                DestroyImmediate(containerDescriptor.AttachedContainer, true);
                DestroyImmediate(containerDescriptor.AttachedContainerGenerator, true);
                DestroyImmediate(containerDescriptor.ContainerSync, true);
                DestroyImmediate(containerDescriptor.OpenableContainer, true);
                DestroyImmediate(containerDescriptor.StorageContainer, true);
                DestroyImmediate(containerDescriptor.VisibleContainer, true);
            }

            ContainerDescriptors.Clear();
        }

        public class ContainerDescriptor
        {
            public GameObject gameObject;
            public AttachedContainer AttachedContainer;
            public AttachedContainerGenerator AttachedContainerGenerator;
            public ContainerSync ContainerSync;
            public OpenableContainer OpenableContainer;
            public StorageContainer StorageContainer;
            public VisibleContainer VisibleContainer;

            // max distance at which the container is visible if not hidden
            private float maxDistance = 5f;
            public float MaxDistance
            {
                get { return maxDistance; }
                set
                {
                    maxDistance = value;
                }
            }

            private ContainerType ContainerType;
            
            public ContainerType HasContainerType
            {
                get { return ContainerType;}
                set 
                { 
                    ContainerType = value;
                    if (value == ContainerType.Normal && VisibleContainer == null)
                    {
                        AddVisible();
                    }

                    if(value == ContainerType.Hidden || value == ContainerType.Pile)
                    {
                        RemoveVisible();
                    }

                    if (IsOpenable)
                    {
                        if(value == ContainerType.Pile) OpenableContainer.SetPile(true);
                        else OpenableContainer.SetPile(false);
                    }
                    if (IsStorage)
                    {
                        StorageContainer.StorageType = value;
                    }
                }
            }

            public bool IsOpenable
            {
                get { return OpenableContainer != null; }
                set
                {
                    if (value && OpenableContainer == null)
                    {
                        RemoveStorage();
                        AddOpenable();
                        if (HasContainerType == ContainerType.Hidden) HasContainerType = ContainerType.Normal;
                    }
                    else if (!value && OpenableContainer != null)
                    {
                        RemoveOpenable();
                        AddStorage();
                    }    
                }
            }
            public bool IsStorage
            {
                get { return StorageContainer != null; }
                set
                {
                    if (value && StorageContainer == null)
                    {
                        AddStorage();
                        RemoveOpenable();
                    }
                    else if (!value && StorageContainer != null)
                    {
                        AddOpenable();
                        RemoveStorage();
                    }
                }
            }

            public ContainerDescriptor(GameObject g)
            {
                gameObject = g;
            }

            public void AddVisible()
            {
                VisibleContainer = gameObject.AddComponent<VisibleContainer>();
                VisibleContainer.AttachedContainer = AttachedContainer;
            }
            public void RemoveVisible()
            {
                DestroyImmediate(VisibleContainer);
            }
            public void AddOpenable()
            {
                OpenableContainer = gameObject.AddComponent<OpenableContainer>();
                OpenableContainer.attachedContainer = AttachedContainer;
                if (IsStorage)
                {
                    DestroyImmediate(StorageContainer, true);
                } 
            }

            public void RemoveOpenable()
            {
                DestroyImmediate(OpenableContainer, true);
            }

            public void AddStorage()
            {
                StorageContainer = gameObject.AddComponent<StorageContainer>();
                StorageContainer.attachedContainer = AttachedContainer;

                if (IsOpenable)
                {
                    DestroyImmediate(OpenableContainer, true);
                }
            }

            public void RemoveStorage()
            {
                DestroyImmediate(StorageContainer, true);
            }

            public void AddGenerator()
            {
                AttachedContainerGenerator = gameObject.AddComponent<AttachedContainerGenerator>();
                AttachedContainerGenerator.AttachedContainer = AttachedContainer;
            }

            public void RemoveGenerator()
            {
                DestroyImmediate(AttachedContainerGenerator, true);
            }

            public void AddAttached()
            {
                AttachedContainer = gameObject.AddComponent<AttachedContainer>();
                AttachedContainer.containerDescriptor = this;
            }

            public void RemoveAttached()
            {
                DestroyImmediate(AttachedContainer, true);
            }

            public void AddSync()
            {
                ContainerSync containerSync = gameObject.GetComponent<ContainerSync>();

                if (containerSync != null)
                { 
                    ContainerSync = containerSync;
                }
                else
                {
                    ContainerSync = gameObject.AddComponent<ContainerSync>();
                }
            }

            public void RemoveSync()
            {
                DestroyImmediate(ContainerSync, true);
            }

        }

    }
   

}