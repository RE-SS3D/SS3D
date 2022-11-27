using FishNet.Connection;
using FishNet.Serializing;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// A custom network serializer for GamemodeObjective.
    /// </summary>
    public static class GamemodeObjectiveSerializer
    {
        /// <summary>
        /// Writes the GamemodeObjective into binary data to be sent in packets.
        /// Has to be done in the order of the variables.
        /// </summary>
        public static void WriteGamemodeObjective(this Writer writer, GamemodeObjective value)
        {
            writer.WriteInt32(value.Id);
            writer.WriteString(value.Title);
            writer.WriteInt16((short)value.Status);
            writer.WriteNetworkConnection(value.Author);
        }

        /// <summary>
        /// Reads binary data and transforms it in a GamemodeObjective.
        /// </summary>
        public static GamemodeObjective ReadGamemodeObjective(this Reader reader)
        {
            int id = reader.ReadInt32();
            string title = reader.ReadString();
            ObjectiveStatus objectiveStatus = (ObjectiveStatus)reader.ReadInt16();
            NetworkConnection author = reader.ReadNetworkConnection();

            GamemodeObjective objective = new(id, title, objectiveStatus, author);

            return objective;
        }
    }
}