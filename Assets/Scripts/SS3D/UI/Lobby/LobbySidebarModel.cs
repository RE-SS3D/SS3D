using SS3D.Systems.Rounds;

namespace SS3D.UI.Lobby
{
    /// <summary>How a lobby status line is coloured.</summary>
    public enum LobbyTone
    {
        Neutral = 0,
        Live = 1,
        Warning = 2,
    }

    /// <summary>The look of an entry button (Ready / Join, Observe).</summary>
    public enum LobbyButtonLook
    {
        Accent = 0,
        Confirmed = 1,
        Plain = 2,
        Disabled = 3,
    }

    /// <summary>Everything the lobby sidebar shows, gathered by the host each refresh.</summary>
    public struct LobbySessionInfo
    {
        public RoundState State;
        public int TimerSeconds;
        public bool AutoStartEnabled;
        public bool AutoStartHeld;
        public int AutoStartSecondsLeft;
        public int Connected;
        public int MinimumConnected;
        public bool EndHeld;
        public int EndSecondsLeft;
        public bool Ready;
        public bool Spawned;
        public bool LateJoinAllowed;
        public bool CanObserve;
        public string FirstName;
    }

    /// <summary>The sidebar's round section (lobby canvas "Sidebar states" 1–8).</summary>
    public readonly struct LobbyRoundStatus
    {
        public readonly string Status;
        public readonly LobbyTone Tone;
        public readonly string Caption;
        /// <summary>The big readout; empty hides it.</summary>
        public readonly string Timer;
        /// <summary>The countdown is held by an admin: the readout is struck through.</summary>
        public readonly bool Held;
        /// <summary>A line under the readout; empty hides it.</summary>
        public readonly string Note;
        public readonly LobbyTone NoteTone;

        public LobbyRoundStatus(string status, LobbyTone tone, string caption, string timer, bool held, string note,
            LobbyTone noteTone)
        {
            Status = status;
            Tone = tone;
            Caption = caption;
            Timer = timer;
            Held = held;
            Note = note;
            NoteTone = noteTone;
        }
    }

    /// <summary>One entry button: title, subtitle, look and whether it can be pressed.</summary>
    public readonly struct LobbyEntryButton
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly LobbyButtonLook Look;
        public readonly bool Enabled;
        /// <summary>A line under the button explaining why it's off; empty hides it.</summary>
        public readonly string Note;

        public LobbyEntryButton(string title, string subtitle, LobbyButtonLook look, bool enabled, string note = "")
        {
            Title = title;
            Subtitle = subtitle;
            Look = look;
            Enabled = enabled;
            Note = note;
        }
    }

    /// <summary>
    /// Pure text and state rules for the lobby sidebar (lobby effort Phase 4): the round section, Ready / Join and
    /// Observe. The view only paints what these return.
    /// </summary>
    public static class LobbySidebarModel
    {
        public static LobbyRoundStatus Round(in LobbySessionInfo info, int lastCountdownSeconds)
        {
            switch (info.State)
            {
                case RoundState.Preparing:
                    return new LobbyRoundStatus("Loading", LobbyTone.Warning, "Loading the station", "", false, "",
                        LobbyTone.Neutral);
                case RoundState.WarmingUp:
                    return new LobbyRoundStatus("Starting", LobbyTone.Warning, "Round starts in",
                        Clock(info.TimerSeconds), false, "", LobbyTone.Neutral);
                case RoundState.Ongoing:
                    return new LobbyRoundStatus("In progress", LobbyTone.Live, "Round time", LongClock(info.TimerSeconds),
                        false, "", LobbyTone.Neutral);
                case RoundState.Ending:
                    return new LobbyRoundStatus("Round over", LobbyTone.Warning, "Back to the lobby in",
                        info.EndHeld ? "" : Clock(info.EndSecondsLeft), false,
                        info.EndHeld ? "The summary is held by an admin" : "", LobbyTone.Warning);
            }

            // Stopped or Ended: the lobby's own countdown (Decision 8).
            if (info.AutoStartEnabled && info.AutoStartSecondsLeft > 0)
                return new LobbyRoundStatus("Pre-round", LobbyTone.Neutral, "Round starts in",
                    Clock(info.AutoStartSecondsLeft), false, "", LobbyTone.Neutral);
            if (info.AutoStartEnabled && info.AutoStartHeld)
                return new LobbyRoundStatus("Pre-round", LobbyTone.Neutral, "Round starts in",
                    lastCountdownSeconds > 0 ? Clock(lastCountdownSeconds) : "--:--", true,
                    "Countdown held by an admin", LobbyTone.Warning);
            if (info.AutoStartEnabled)
            {
                int minimum = info.MinimumConnected < 1 ? 1 : info.MinimumConnected;
                return new LobbyRoundStatus("Pre-round", LobbyTone.Neutral, "Waiting for players", "", false,
                    $"{info.Connected}/{minimum} connected. The countdown starts at {minimum}.", LobbyTone.Neutral);
            }

            return new LobbyRoundStatus("Pre-round", LobbyTone.Neutral, "Waiting for an admin", "", false,
                "An admin starts the round on this server.", LobbyTone.Neutral);
        }

        /// <summary>Ready before the round, Join during it (lobby.md §7–§8).</summary>
        public static LobbyEntryButton Entry(in LobbySessionInfo info)
        {
            string name = string.IsNullOrWhiteSpace(info.FirstName) ? "your character" : info.FirstName;
            if (info.State == RoundState.Ongoing)
            {
                if (info.Spawned)
                    return new LobbyEntryButton("In round", "You already have a body this round", LobbyButtonLook.Disabled,
                        false);
                if (!info.LateJoinAllowed)
                    return new LobbyEntryButton("Join", "Late joining is off", LobbyButtonLook.Disabled, false,
                        "An admin has turned off late joining for this round. You can still observe.");
                return new LobbyEntryButton("Join", "Pick an open job and spawn now", LobbyButtonLook.Accent, true);
            }

            if (info.State == RoundState.Ending)
                return new LobbyEntryButton("Ready up", "The round is ending", LobbyButtonLook.Disabled, false);
            if (info.Spawned)
                return new LobbyEntryButton("In round", "You already have a body this round", LobbyButtonLook.Disabled,
                    false);
            if (info.Ready)
                return new LobbyEntryButton("Ready", "You'll spawn at round start. Click to cancel",
                    LobbyButtonLook.Confirmed, true);
            return new LobbyEntryButton("Ready up", $"Spawn as {name} when the round starts", LobbyButtonLook.Accent, true);
        }

        /// <summary>Observe is immediate (Decision 9), open only while a station is loaded and the player has no body.</summary>
        public static LobbyEntryButton Observe(in LobbySessionInfo info)
        {
            if (info.Spawned)
                return new LobbyEntryButton("Observe", "You're already in the round", LobbyButtonLook.Disabled, false);
            if (info.CanObserve)
                return new LobbyEntryButton("Observe", "Go in now as a ghost, no body", LobbyButtonLook.Plain, true);
            return new LobbyEntryButton("Observe", "Opens once the station has loaded", LobbyButtonLook.Disabled, false);
        }

        /// <summary>The header's round word: the same status the sidebar shows.</summary>
        public static string HeaderRound(in LobbySessionInfo info) => Round(info, 0).Status;

        public static string Clock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        public static string LongClock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
        }

        /// <summary>The first word of a character name, for "Spawn as Rory".</summary>
        public static string FirstName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            string trimmed = name.Trim();
            int space = trimmed.IndexOf(' ');
            return space > 0 ? trimmed.Substring(0, space) : trimmed;
        }
    }
}
