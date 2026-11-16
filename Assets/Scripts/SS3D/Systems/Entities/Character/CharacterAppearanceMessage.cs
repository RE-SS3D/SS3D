using FishNet.Broadcast;

namespace SS3D.Systems.Entities.Character
{
    /// <summary>
    /// Client → server: the sender's saved lobby character. The server resolves the player from
    /// the sending connection and applies it to their next spawned body.
    /// </summary>
    public struct CharacterAppearanceMessage : IBroadcast
    {
        public CharacterAppearance Appearance;
    }
}
