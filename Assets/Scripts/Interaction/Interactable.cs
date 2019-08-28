    using UnityEngine;
    using UnityEngine.Events;

    public class Interactable : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent clickedEvent;

        public void Click()
        {
            clickedEvent.Invoke();
        }
    }
