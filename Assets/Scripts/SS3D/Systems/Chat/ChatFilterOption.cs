using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    public class ChatFilterOption : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label = null;
        [SerializeField] private Toggle _toggle = null;

        private ChatChannel _channel;

        public void Init(ChatChannel channel)
        {
            _channel = channel;
            _label.text = channel.name;
        }

        public ChatChannel TickedChannel() => _toggle.isOn ? _channel : null;
    }
}