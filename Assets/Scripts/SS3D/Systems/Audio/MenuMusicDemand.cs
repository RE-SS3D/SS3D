namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Who wants menu music right now (lobby effort Decision 15). The main menu and the lobby each say whether
    /// their screen wants it. A join hands over from one to the other: the music keeps playing through the session
    /// load until the lobby reports, so there's no dip between the menu hiding and the lobby showing.
    /// </summary>
    public sealed class MenuMusicDemand
    {
        /// <summary>The longest a handover keeps the music playing with no word from the lobby.</summary>
        public const float HandoverSeconds = 30f;

        private bool _mainMenu;
        private bool _lobby;
        private float? _handoverUntil;

        /// <summary>The main menu's screen wants music (menu, server list, settings, connecting).</summary>
        public void SetMainMenu(bool wanted)
        {
            _mainMenu = wanted;
            if (wanted) _handoverUntil = null;
        }

        /// <summary>
        /// The main menu is leaving for a session. If it was playing, keep playing until the lobby reports or
        /// <see cref="HandoverSeconds"/> pass.
        /// </summary>
        public void HandOverToLobby(float now)
        {
            if (_mainMenu) _handoverUntil = now + HandoverSeconds;
            _mainMenu = false;
        }

        /// <summary>
        /// The lobby's view of the local player: null while not connected to a session (no opinion yet, so a
        /// handover keeps going), true in the lobby, false once they have a body or a ghost.
        /// </summary>
        public void SetLobby(bool? inLobby)
        {
            _lobby = inLobby == true;
            if (inLobby.HasValue) _handoverUntil = null;
        }

        public bool Wanted(float now)
        {
            if (_handoverUntil is { } until && now >= until) _handoverUntil = null;
            return _mainMenu || _lobby || _handoverUntil.HasValue;
        }
    }
}
