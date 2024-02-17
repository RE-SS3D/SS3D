using UnityEngine;

namespace SS3D.Engine.Chat
{
    public class ToggleChats : MonoBehaviour
    {
        [SerializeField]
        private Transform chatWindowsContainer;
        
        public void ToggleAllChats()
        {
            foreach (ChatWindow chatWindow in chatWindowsContainer.GetComponentsInChildren<ChatWindow>())
            {
                chatWindow.ToggleChatWindowUI();
            }
        }
    }
}