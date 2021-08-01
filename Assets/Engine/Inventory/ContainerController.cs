
using UnityEngine;
using SS3D.Content.Furniture.Storage;

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
        // can either be Openable or Storage, not both.
        public bool isOpenable;
        public bool addedContainer = false;
        public bool hasAttachedContainer => gameObject.GetComponent<AttachedContainer>() != null;
        public bool hasOpenableContainer => gameObject.GetComponent<OpenableContainer>() != null;

        // if is normal, has an UI, if Pile or Hidden, has no UI.
        // Visible script only attached on container with an UI. 
        // Warning, Pile container can have an UI when there is multiple containers on the same game object, in the context menu. 
        public ContainerType containerType;

        public Vector2Int Size;


        public void Start()
        {
           /* AttachedContainer attachedContainer = gameObject.AddComponent<AttachedContainer>();
            gameObject.AddComponent<AttachedContainerGenerator>().initialize(attachedContainer, Size);
            gameObject.AddComponent<ContainerSync>();

            if (isOpenable)
            {
                gameObject.AddComponent<OpenableContainer>();
            }
            else
            {
                gameObject.AddComponent<StorageContainer>();
            }

            if (containerType == ContainerType.Normal | containerType == ContainerType.Pile)
            {
                gameObject.AddComponent<VisibleContainer>(); 
            } */
        }

        public void AddBaseComponent()
        {
            AttachedContainer attachedContainer = gameObject.AddComponent<AttachedContainer>();
            AttachedContainerGenerator attachedContainerGenerator = gameObject.AddComponent<AttachedContainerGenerator>();
            ContainerSync containerSync = gameObject.AddComponent<ContainerSync>();
        }

        public void RemoveBaseComponent()
        {
            DestroyImmediate(GetComponent(typeof(AttachedContainer)));
            DestroyImmediate(GetComponent(typeof(AttachedContainerGenerator)));
            DestroyImmediate(GetComponent(typeof(ContainerSync)));
        }

        public void AddAttachedContainer()
        {
            AttachedContainer attachedContainer = gameObject.AddComponent<AttachedContainer>();   
        }
        public void RemoveAttachedContainer()
        {
                 DestroyImmediate(GetComponent(typeof(AttachedContainer)));
        }
        public void AddOpenableContainer()
        {
            OpenableContainer attachedContainer = gameObject.AddComponent<OpenableContainer>();
        }
        public void RemoveOpenableContainer()
        {
            DestroyImmediate(GetComponent(typeof(OpenableContainer)));
        }



    }
}