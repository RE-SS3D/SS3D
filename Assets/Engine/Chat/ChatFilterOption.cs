using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    public class ChatFilterOption : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label = null;
        [SerializeField] private Toggle toggle = null;

        private ChatChannel channel;

        public void Init(ChatChannel channel)
        {
            this.channel = channel;
            label.text = channel.Name;
        }

        public ChatChannel TickedChannel() => toggle.isOn ? channel : new ChatChannel();
    }
}