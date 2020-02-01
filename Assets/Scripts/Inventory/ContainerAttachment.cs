using UnityEngine;

namespace Inventory
{
    /**
     * Handles the attachment between a container and the inventory.
     * 
     * This component should only exist on the server.
    */
    public class ContainerAttachment : MonoBehaviour
    {
        public Inventory   inventory = null;
        public Container   container = null;
        public float       range = 5.0f;

        public void Awake()
        {
            if(!inventory)
                inventory = GetComponent<Inventory>();

            if(!inventory) {
                Debug.LogError("ContainerAttachment component cannot find inventory");
            }
        }
        public void Start()
        {
            inventory.AddContainer(container.gameObject);
        }

        public void Update()
        {
            var distance = inventory.transform.position - container.transform.position;

            if (distance.sqrMagnitude > (range * range))
            {
                inventory.RemoveContainer(container.gameObject);
                Destroy(this);
            }
            if(!inventory.HasContainer(container.gameObject))
                Destroy(this);
        }
    }
}
