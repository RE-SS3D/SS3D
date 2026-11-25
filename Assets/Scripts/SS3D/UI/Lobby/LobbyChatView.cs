using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Permissions;
using SS3D.Systems.Comms;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
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
        /// <summary>
        /// More lines than this arriving in one snapshot are a history replay (connecting, the admin log), not someone
        /// speaking: they appear without sliding in.
        /// </summary>
        private const int MaxSlidingLines = 4;

        /// <summary>Lines arriving together slide in this far apart.</summary>
        private const long SlideStepMilliseconds = 60;

        private readonly InputTextEntryScope _textEntry;
        private readonly ScrollView _log;
        private readonly TextField _input;
        private readonly Button _adminChannel;
        private readonly VisualElement _composer;
        private readonly Dictionary<OocChannel, Button> _channels = new();
        private OocChannel _channel = OocChannel.Ooc;
        private OocChatMessage[] _messages = Array.Empty<OocChatMessage>();
        private int _version = -1;
        private bool _admin;
        private bool _connected;
        /// <summary>A real snapshot has been painted; lines added after it are news and slide in.</summary>
        private bool _painted;
        /// <summary>"No messages yet." while the log shows no line; the first line replaces it.</summary>
        private Label _empty;

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

            // The channels over the field, so the field keeps the narrow panel's width; the field's edge and the
            // chosen channel take the channel's colour (lobby-chat__composer--<channel>).
            _composer = LobbyUi.Box("lobby-chat__composer");
            _composer.AddToClassList("lobby-chat__composer--stacked");
            VisualElement composer = _composer;
            VisualElement channels = LobbyUi.Box("lobby-chat__composer-row");
            composer.Add(channels);
            AddChannel(channels, OocChannel.Ooc, "OOC");
            AddChannel(channels, OocChannel.Looc, "LOOC");
            _adminChannel = AddChannel(channels, OocChannel.Admin, "Admin");
            VisualElement line = LobbyUi.Box("lobby-chat__composer-row");
            line.AddToClassList("lobby-chat__composer-row--field");
            composer.Add(line);
            _adminChannel.style.display = DisplayStyle.None;

            _input = new TextField { name = "chat-input", maxLength = OocChatRules.MaxLength, label = string.Empty };
            _input.AddToClassList("lobby-chat__input");
            if (_input.textEdition != null) _input.textEdition.hidePlaceholderOnFocus = true;
            // TrickleDown: a real Return targets the inner text element, which consumes it before it bubbles to the field.
            // Enter arrives as two key events, the key and then its '\n' character. A single-line field ends its edit on
            // the character (focus moves to the field's root and typing stops), so both are taken here; the second
            // Send finds the field empty.
            _input.RegisterCallback<KeyDownEvent>(evt =>
            {
                bool enterKey = evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter;
                bool enterCharacter = evt.keyCode == KeyCode.None && evt.character is '\n' or '\r' or '\u0003';
                if (!enterKey && !enterCharacter) return;
                Send();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            // While typing, Enter's navigation submit has nothing to do either (on the field's root, it starts an edit).
            _input.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                if (evt.target != _input) evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            LobbyUi.EditorInput(_input);
            LobbyUi.HoldTextEntry(_input, _textEntry);
            line.Add(_input);

            Button send = new(Send) { name = "chat-send", tooltip = "Send (Enter)" };
            send.AddToClassList("lobby-chat__send");
            send.Add(LobbyUi.Icon("send"));
            send.Add(LobbyUi.Titling("Send", "lobby-chat__send-label"));
            line.Add(send);
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
            button.AddToClassList("lobby-chat__channel-button--" + channel.ToString().ToLowerInvariant());
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
            foreach (OocChannel each in new OocChannel[] { OocChannel.Ooc, OocChannel.Looc, OocChannel.Admin })
                _composer.EnableInClassList("lobby-chat__composer--" + each.ToString().ToLowerInvariant(), each == channel);
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
            KeepTyping();
        }

        /// <summary>
        /// The field stays ready for the next line, whether Enter or the Send button sent this one: focused again on the
        /// next tick (the button takes focus on its click), unless the chat has gone from view by then.
        /// </summary>
        private void KeepTyping()
        {
            _input.schedule.Execute(() =>
            {
                if (_input.panel == null || !_input.enabledInHierarchy || _input.resolvedStyle.display == DisplayStyle.None)
                {
                    return;
                }

                _input.Focus();
            });
        }

        /// <summary>
        /// Replaces the log snapshot; the log changes only when the version does. Lines added at the end are appended,
        /// and a few at a time (someone speaking) slide in; anything else rebuilds the log in place.
        /// </summary>
        public void SetChat(IEnumerable<OocChatMessage> log, int version, bool admin, bool connected)
        {
            _connected = connected;
            bool rebuild = false;
            if (admin != _admin)
            {
                _admin = admin;
                _adminChannel.style.display = admin ? DisplayStyle.Flex : DisplayStyle.None;
                if (!admin && _channel == OocChannel.Admin) SelectChannel(OocChannel.Ooc);
                _version = -1;
                rebuild = true;
            }

            if (version == _version) return;
            _version = version;
            OocChatMessage[] before = _messages;
            _messages = log.ToArray();
            if (!rebuild && _painted && Extends(before, _messages)) Append(before.Length);
            else Rebuild();
            _painted = true;
        }

        /// <summary>The new snapshot is the old one with lines added at its end (ids are unique and ordered).</summary>
        private static bool Extends(OocChatMessage[] before, OocChatMessage[] after)
        {
            if (before.Length == 0) return true;
            return after.Length >= before.Length && after[0].Id == before[0].Id
                && after[before.Length - 1].Id == before[before.Length - 1].Id;
        }

        /// <summary>Adds the lines from <paramref name="from"/> on; a few slide in, a burst just appears.</summary>
        private void Append(int from)
        {
            bool follow = AtBottom();
            int added = 0;
            for (int i = from; i < _messages.Length; i++)
                if (Shows(_messages[i])) added++;
            if (added == 0)
            {
                if (_empty != null) _empty.text = EmptyText;
                return;
            }

            _empty?.RemoveFromHierarchy();
            _empty = null;
            bool slide = added <= MaxSlidingLines;
            int index = 0;
            for (int i = from; i < _messages.Length; i++)
            {
                if (!Shows(_messages[i])) continue;
                VisualElement row = Line(_messages[i]);
                _log.Add(row);
                if (slide) UiMotion.Enter(row, index * SlideStepMilliseconds);
                index++;
            }

            if (follow) FollowEnd();
        }

        private void Rebuild()
        {
            bool follow = AtBottom();
            _log.Clear();
            _empty = null;
            bool any = false;
            foreach (OocChatMessage message in _messages)
            {
                if (!Shows(message)) continue;
                _log.Add(Line(message));
                any = true;
            }

            if (!any)
            {
                _empty = LobbyUi.Text(EmptyText, "lobby-chat__empty");
                _log.Add(_empty);
            }

            if (follow) FollowEnd();
        }

        private string EmptyText => _connected ? "No messages yet." : "Not connected.";

        private bool Shows(OocChatMessage message) => message.Channel != OocChannel.Admin || _admin;

        /// <summary>Follow new lines only while the reader is already at the bottom.</summary>
        private bool AtBottom() => _log.panel == null || _log.scrollOffset.y >= _log.verticalScroller.highValue - 4f;

        private void FollowEnd() =>
            _log.schedule.Execute(() => _log.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);

        private static VisualElement Line(OocChatMessage message)
        {
            VisualElement row = LobbyUi.Box("lobby-chat__line");
            // Admin lines are an area in the admin colour (Lobby.uss), so they stand out of the scroll.
            row.EnableInClassList("lobby-chat__line--admin", message.Channel == OocChannel.Admin);
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
