using FishNet.Broadcast;

namespace SS3D.Systems.GameModes.Events
{
    /// <summary>
    /// A round-wide gamemode announcement sent from the server to every client
    /// (for example "The traitors have won!"). Unlike GamemodeObjectiveUpdatedMessage,
    /// this is broadcast to all clients rather than only the objective owner.
    /// </summary>
    public struct GamemodeAnnouncementMessage : IBroadcast
    {
        public readonly string Message;

        public GamemodeAnnouncementMessage(string message)
        {
            Message = message;
        }
    }
}