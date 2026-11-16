namespace SS3D.Systems.Comms
{
    /// <summary>
    /// How a local speech line was uttered. Whisper/Shout/Emote are compose modifiers.
    /// Radio and Announcement exist for subtitle CSS preview only — live radio/announce
    /// use <see cref="CommsMessage"/> and the non-diegetic feed.
    /// </summary>
    public enum SpeechMode : byte
    {
        Speak = 0,
        Whisper = 1,
        Shout = 2,
        Emote = 3,
        Radio = 4,
        Announcement = 5,

        /// <summary>A narrated action line (action-messages.md §5): local only, never sent as speech.</summary>
        Action = 6,
    }
}
