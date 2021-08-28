
using UnityEngine;
using SS3D.Content.Furniture.Storage;
using System.Collections.Generic;

namespace SS3D.Engine.Inventory
{

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

        // if is normal, has an UI, if Pile or Hidden, has no UI.
        // Visible script only attached on container with an UI. 
        // Warning, Pile container can have an UI when there is multiple containers on the same game object, in the context menu. 
        public ContainerType containerType;

        public void AddBaseComponent()
        {
            ContainerDescriptor ContainerDescriptor = new ContainerDescriptor();

            AttachedContainer AttachedContainer = gameObject.AddComponent<AttachedContainer>();
            ContainerDescriptor.AttachedContainer = AttachedContainer;

            AttachedContainerGenerator AttachedContainerGenerator = gameObject.AddComponent<AttachedContainerGenerator>();
            AttachedContainerGenerator.AttachedContainer = AttachedContainer;
            ContainerDescriptor.AttachedContainerGenerator = AttachedContainerGenerator;
            
            if (ContainerDescriptors.Count == 0)
            {
                ContainerSync ContainerSync = gameObject.AddComponent<ContainerSync>();
                ContainerDescriptor.ContainerSync = ContainerSync;
            }
            else
            {
                ContainerDescriptor.ContainerSync = gameObject.GetComponent<ContainerSync>();
            }

            ContainerDescriptor.gameObject = gameObject;
            ContainerDescriptors.Add(ContainerDescriptor);
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
            DestroyImmediate(ContainerDescriptors[IndexOf].AttachedContainer);
            DestroyImmediate(ContainerDescriptors[IndexOf].AttachedContainerGenerator);
            if(ContainerDescriptors.Count == 1)
            {
                DestroyImmediate(ContainerDescriptors[IndexOf].ContainerSync);
            }
            DestroyImmediate(ContainerDescriptors[IndexOf].OpenableContainer);
            DestroyImmediate(ContainerDescriptors[IndexOf].StorageContainer);
            ContainerDescriptors.RemoveAt(IndexOf);
        }

        public void RemoveAllContainers()
        {
            foreach(ContainerDescriptor containerDescriptor in ContainerDescriptors)
            {
                DestroyImmediate(containerDescriptor.AttachedContainer);
                DestroyImmediate(containerDescriptor.AttachedContainerGenerator);
                DestroyImmediate(containerDescriptor.ContainerSync);
                DestroyImmediate(containerDescriptor.OpenableContainer);
                DestroyImmediate(containerDescriptor.StorageContainer);
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

            private ContainerType ContainerType;
            public ContainerType HasContainerType
            {
                get { return ContainerType;}
                set { ContainerType = value; }
            }

            public bool IsOpenable
            {
                get { return OpenableContainer != null; }
                set
                {
                    if (value && OpenableContainer == null)
                    {
                        AddOpenable();
                    }
                    else if (!value && OpenableContainer != null) RemoveOpenable();
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
                    }
                    else if (!value && StorageContainer != null) RemoveStorage();
                }
            }

            public void AddOpenable()
            {
                OpenableContainer = gameObject.AddComponent<OpenableContainer>();
                if (IsStorage)
                {
                    DestroyImmediate(StorageContainer);
                } 
            }

            public void RemoveOpenable()
            {
                DestroyImmediate(OpenableContainer);
            }

            public void AddStorage()
            {
                StorageContainer = gameObject.AddComponent<StorageContainer>();
                if (IsOpenable)
                {
                    DestroyImmediate(OpenableContainer);
                }
            }

            public void RemoveStorage()
            {
                DestroyImmediate(StorageContainer);
            }
        }

    }
   

}