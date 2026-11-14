using FishNet.Broadcast;

namespace SS3D.Systems.Roles.Messages
{
    /// <summary>
    /// Client → server: the selected character's saved job priorities, antagonist choices and fallback (lobby.md
    /// §3–§5). Sent on connect, on selecting a character and on saving one; the server resolves the player from the
    /// sending connection and keeps the latest per ckey.
    /// </summary>
    public struct JobPreferencesMessage : IBroadcast
    {
        public string[] Jobs;
        public byte[] Priorities;
        /// <summary>Antagonist categories the character opted into.</summary>
        public string[] AntagonistOptIns;
        /// <summary>
        /// Categories the character opted out of. A category in neither list takes the server default
        /// (<c>ServerSettings.AntagonistOptInDefault</c>): off, except on the <c>-nukeops</c> profile.
        /// </summary>
        public string[] AntagonistOptOuts;
        /// <summary>A <see cref="JobFallback"/>: what the character does when none of its jobs is open.</summary>
        public byte Fallback;
    }
}
