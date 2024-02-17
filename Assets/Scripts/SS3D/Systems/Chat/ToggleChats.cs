using UnityEngine;

namespace SS3D.Engine.Chat
{
    public class ToggleChat : MonoBehaviour
    {
        public void ToggleChat()
        {
            
            foreach (ChatWindow chatWindow in transform.GetComponentsInParent<ChatWindow>())
            {
                chatWindow.ToggleChatWindowUI();
            }
        }
    }
}