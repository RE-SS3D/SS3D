using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using SS3D.Engine.Chat;

namespace SS3D.Engine.Inventory.UI
{
    /// <summary>
    /// Allows the chat user interface to be collapsible by clicking a button.
    /// </summary>
    public class ToggleChat : MonoBehaviour
    {
        private Button ExpandButton;
		private GameObject ChatWindow;

        void Start()
        {
            ExpandButton = GetComponent<Button>();
            ExpandButton.onClick.AddListener(TaskOnClick);

            // Go ahead and hide it on the start
            Toggle();
        }
		
        public void TaskOnClick()
        {
            Toggle();
        }

        /// <summary>
        /// Toggle the Chat UI using transparency and turning on or off the ability to block ray casts.
        /// </summary>
        private void Toggle()
        {
            if(!ChatWindow)
			{
				ChatWindow = GameObject.FindGameObjectWithTag("ChatWindow");	
            }
            else
            {
                ChatWindow.GetComponent<ChatWindow>().ToggleChatWindowUI();
            }
        }
    }
}