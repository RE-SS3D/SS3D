using System;
using TMPro;
using UnityEngine;

namespace SS3D.Content.Graphics.UI
{
    public class Menu : MonoBehaviour
    {
        public string Title
        {
            get => titleElement.text;
            set
            {
                titleElement.SetText(value);
                name = value;
            }
        }

        public event EventHandler Closed;

        [SerializeField]
        private TMP_Text titleElement;

        /// <summary>
        /// Closes this menu
        /// </summary>
        public void Close()
        {
            Destroy(gameObject);
            OnClosed();
        }
    
        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
