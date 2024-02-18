﻿using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    public class ChatFilterOption : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label = null;
        [SerializeField] private Toggle toggle = null;

        private ChatChannel _channel;

        public void Init(ChatChannel channel)
        {
            _channel = channel;
            label.text = channel.name;
        }

        public ChatChannel TickedChannel() => toggle.isOn ? _channel : null;
    }
}