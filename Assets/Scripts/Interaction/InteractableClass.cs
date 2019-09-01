    using UnityEngine;
    using UnityEngine.Events;

    public class InteractableClass : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent clickedEvent;

        public void Click()
        {
            clickedEvent.Invoke();
        }
    }
