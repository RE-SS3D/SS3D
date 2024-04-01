using SS3D.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    public class ToggleChats : MonoBehaviour
    {
        public void ToggleAllChats()
        {
            List<InGameChatWindow> allChatWindows = ViewLocator.Get<InGameChatWindow>();
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