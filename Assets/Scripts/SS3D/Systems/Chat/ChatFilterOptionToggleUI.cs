using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    public class ChatFilterOptionToggleUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label = null;
        [SerializeField] private Toggle _toggle = null;

        private ChatChannel _chatChannel;

        public void Setup(ChatChannel chatChannel)
        {
            _chatChannel = chatChannel;
            _label.text = chatChannel.name;
        }

        public bool TryGetTickedChannel(out ChatChannel chatChannel)
        {
            if (_toggle.isOn)
            {
                chatChannel = _chatChannel;
                return true;
            }

            chatChannel = null;
            return false;
        }
    }
}