using System;
using System.Collections.Generic;

namespace SS3D.UI.Shell.MainMenu
{
    public enum MainMenuScreen { Menu = 0, Servers = 1, Settings = 2, Connecting = 3, Hosting = 4, Updating = 5 }
    public enum MainMenuPublicListState { Populated = 0, Empty = 1, FetchFailed = 2 }
    public enum MainMenuServerState { Compatible = 0, NeedsUpdate = 1, Full = 2, Offline = 3, OlderVersion = 4, Checking = 5 }

    /// <summary>Buttons and links the view reports; server commands carry the server id.</summary>
    public enum MainMenuCommand
    {
        Join = 0,
        PlayLocally = 1,
        ToggleAdvanced = 2,
        Settings = 3,
        Quit = 4,
        Back = 5,
        Refresh = 6,
        Discord = 7,
        Credits = 8,
        Connect = 9,
        Update = 10,
        Cancel = 11,
        ServerJoin = 12,
        ServerUpdate = 13,
        ServerDetails = 14,
        ServerRemove = 15,
        ToggleMusic = 16,
        NextTrack = 17,
        Retry = 18,
    }

    /// <summary>Editable fields the view reports; server fields carry the server id.</summary>
    public enum MainMenuField { PlayerName = 0, Port = 1, Address = 2, ServerName = 3, AdminToken = 4, MusicVolume = 5, OperationToken = 6 }

    /// <summary>Presentation data only. Does not query servers or persist credentials.</summary>
    public sealed class MainMenuPresentation
    {
        public MainMenuScreen Screen;
        public string PlayerName = "";
        public string Version = "";
        public string LatestVersion = "";
        public string UpdateMessage = "";
        public bool UpdateAvailable;
        /// <summary>False outside an extracted release: the update button opens the download page instead.</summary>
        public bool CanSelfUpdate = true;
        /// <summary>Why the last local session failed to start; replaces the Play Locally subtitle.</summary>
        public string LocalError = "";
        public bool MusicOn = true;
        /// <summary>Current track; empty when the catalog has no music.</summary>
        public string MusicTitle = "";
        /// <summary>Linear 0..1 (the player's Music volume).</summary>
        public float MusicVolume = 0.8f;
        public bool FirstLaunch;
        public bool Advanced;
        public string Port = "1151";
        public string Address = "";
        public string AddressError = "";
        public string CheckedText = "Checked just now";
        public int Wallpaper;
        /// <summary>Live frame of a video wallpaper; null shows the still for <see cref="Wallpaper"/>.</summary>
        public UnityEngine.Texture VideoWallpaper;
        /// <summary>Black over the wallpaper while it changes.</summary>
        public bool WallpaperVeiled;
        public MainMenuPublicListState PublicList;
        public IReadOnlyList<MainMenuServerPresentation> Recent = Array.Empty<MainMenuServerPresentation>();
        public IReadOnlyList<MainMenuServerPresentation> Public = Array.Empty<MainMenuServerPresentation>();
        public string OperationName = "";
        public string OperationAddress = "";
        public int OperationStep;
        /// <summary>The Connecting dialog shows a failure: Retry and Back replace Cancel.</summary>
        public bool OperationFailed;
        /// <summary>A session dropped: the dialog is titled CONNECTION LOST and shows no steps.</summary>
        public bool OperationLost;
        public string OperationReason = "";
        /// <summary>The server asked for an admin token: the dialog offers a field for one.</summary>
        public bool OperationNeedsToken;
        public string OperationToken = "";
    }

    public sealed class MainMenuServerPresentation
    {
        public string Id = "";
        public string Name = "";
        public string Address = "";
        public MainMenuServerState State;
        public string RequiredVersion = "";
        public string Players = "—";
        public string Ping = "—";
        public string Refusal = "";
        public string RefusalTime = "";
        public bool Expanded;
        public string AdminToken = "";
    }
}
