using UnityEngine;

namespace SS3D.Engine.Chat
{
    public class ToggleChats : MonoBehaviour
    {
        [SerializeField]
        private Transform chatWindowsContainer;
        
        public void ToggleAllChats()
        {
            InGameChatWindow[] allChatWindows = chatWindowsContainer.GetComponentsInChildren<InGameChatWindow>();
            bool hideChats = allChatWindows[0].CanvasGroup.alpha >= 1.0f;
            foreach (InGameChatWindow chatWindow in allChatWindows)
            {
                if (hideChats)
                {
                    chatWindow.HideChatWindowUI();
                }
                else
                {
                    chatWindow.ShowChatWindowUI();
                }
            }
        }
    }
}