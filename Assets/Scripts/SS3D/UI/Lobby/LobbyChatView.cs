using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Permissions;
using SS3D.Systems.Comms;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The lobby's Chat tab (comms.md §7): one log of every out-of-character channel the player can read, channel
    /// buttons that pick where a line goes, and a one-line composer. Admin chat is shown only to admins; the server
    /// accepts it only from them.
    /// </summary>
    public sealed class LobbyChatView
    {
        private readonly InputTextEntryScope _textEntry;
        private readonly ScrollView _log;
        private readonly TextField _input;
        private readonly Button _adminChannel;
        private readonly Dictionary<OocChannel, Button> _channels = new();
        private OocChannel _channel = OocChannel.Ooc;
        private OocChatMessage[] _messages = Array.Empty<OocChatMessage>();
        private int _version = -1;
        private bool _admin;
        private bool _connected;

        public event Action<OocChannel, string> Submitted;

        public VisualElement Root { get; }

        public LobbyChatView(InputTextEntryScope textEntry)
        {
            _textEntry = textEntry;
            Root = LobbyUi.Box("lobby-chat");
            Root.name = "chat";

            _log = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            _log.name = "chat-log";
            _log.AddToClassList("lobby-chat__log");
            Root.Add(_log);

            VisualElement composer = LobbyUi.Box("lobby-chat__composer");
            AddChannel(composer, OocChannel.Ooc, "OOC");
            AddChannel(composer, OocChannel.Looc, "LOOC");
            _adminChannel = AddChannel(composer, OocChannel.Admin, "Admin");
            _adminChannel.style.display = DisplayStyle.None;

            _input = new TextField { name = "chat-input", maxLength = OocChatRules.MaxLength, label = string.Empty };
            _input.AddToClassList("lobby-chat__input");
            if (_input.textEdition != null) _input.textEdition.hidePlaceholderOnFocus = true;
            // TrickleDown: a real Return targets the inner text element, which consumes it before it bubbles to the field.
            _input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
                Send();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            LobbyUi.DarkInput(_input);
            LobbyUi.HoldTextEntry(_input, _textEntry);
            composer.Add(_input);

            Button send = new(Send) { name = "chat-send", tooltip = "Send (Enter)" };
            send.AddToClassList("lobby-chat__send");
            send.Add(LobbyUi.Icon("send"));
            send.Add(LobbyUi.Titling("Send", "lobby-chat__send-label"));
            composer.Add(send);
            Root.Add(composer);

            Root.Add(LobbyUi.Text("Enter sends to the selected channel. LOOC reaches players near your character.",
                "lobby-chat__hint"));
            SelectChannel(OocChannel.Ooc);
            Rebuild();
        }

        private Button AddChannel(VisualElement parent, OocChannel channel, string label)
        {
            Button button = new(() => SelectChannel(channel))
            {
                name = $"chat-channel-{channel.ToString().ToLowerInvariant()}",
                text = label.ToUpperInvariant(),
            };
            button.AddToClassList("lobby-chat__channel-button");
            button.AddToClassList("lb-titling");
            _channels[channel] = button;
            parent.Add(button);
            return button;
        }

        private void SelectChannel(OocChannel channel)
        {
            _channel = channel;
            foreach ((OocChannel key, Button button) in _channels)
                button.EnableInClassList("lobby-chat__channel-button--active", key == channel);
            if (_input.textEdition != null)
                _input.textEdition.placeholder = channel switch
                {
                    OocChannel.Looc => "Say something to players near your character",
                    OocChannel.Admin => "Message the other admins",
                    _ => "Say something out of character",
                };
        }

        private void Send()
        {
            string text = _input.value?.Trim() ?? string.Empty;
            if (text.Length == 0) return;
            Debug.Log($"[Lobby] Chat send ({_channel.ToString().ToLowerInvariant()}): {text}");
            Submitted?.Invoke(_channel, text);
            _input.value = string.Empty;
        }

        /// <summary>Replaces the log snapshot; the log rebuilds only when the version changes.</summary>
        public void SetChat(IEnumerable<OocChatMessage> log, int version, bool admin, bool connected)
        {
            _connected = connected;
            if (admin != _admin)
            {
                _admin = admin;
                _adminChannel.style.display = admin ? DisplayStyle.Flex : DisplayStyle.None;
                if (!admin && _channel == OocChannel.Admin) SelectChannel(OocChannel.Ooc);
                _version = -1;
            }

            if (version == _version) return;
            _version = version;
            _messages = log.ToArray();
            Rebuild();
        }

        private void Rebuild()
        {
            // Follow new lines only while the reader is already at the bottom.
            bool follow = _log.panel == null || _log.scrollOffset.y >= _log.verticalScroller.highValue - 4f;
            _log.Clear();
            bool any = false;
            foreach (OocChatMessage message in _messages)
            {
                if (message.Channel == OocChannel.Admin && !_admin) continue;
                _log.Add(Line(message));
                any = true;
            }

            if (!any) _log.Add(LobbyUi.Text(_connected ? "No messages yet." : "Not connected.", "lobby-chat__empty"));
            if (!follow) return;
            _log.schedule.Execute(() => _log.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);
        }

        private static VisualElement Line(OocChatMessage message)
        {
            VisualElement row = LobbyUi.Box("lobby-chat__line");
            string time = DateTimeOffset.FromUnixTimeSeconds(message.UnixSeconds).ToLocalTime().ToString("HH:mm");
            row.Add(LobbyUi.Mono(time, "lobby-chat__time"));
            Label channel = LobbyUi.Text(ChannelTag(message.Channel), "lobby-chat__channel");
            channel.AddToClassList("lobby-chat__channel--" + message.Channel.ToString().ToLowerInvariant());
            row.Add(channel);

            VisualElement body = LobbyUi.Box("lobby-chat__body");
            if (message.IsSystem)
            {
                body.Add(LobbyUi.Text(message.Text, "lobby-chat__system"));
            }
            else
            {
                // Player-supplied text: never interpret rich-text tags.
                Label name = LobbyUi.Text($"{message.Sender}:", "lobby-chat__name");
                name.EnableInClassList("lobby-chat__name--admin", message.SenderRole >= ServerRoleTypes.Administrator);
                body.Add(name);
                body.Add(LobbyUi.Text(message.Text, "lobby-chat__text"));
            }

            row.Add(body);
            return row;
        }

        private static string ChannelTag(OocChannel channel) => channel switch
        {
            OocChannel.Looc => "LOOC",
            OocChannel.Admin => "ADMIN",
            _ => "OOC",
        };
    }
}
