using FishNet.Connection;
using FishNet.Serializing;

namespace SS3D.Systems.GameModes.Objectives
{
    public static class GamemodeObjectiveSerializer
    {
        public static void WriteGamemodeObjective(this Writer writer, GamemodeObjective value)
        {
            writer.WriteInt32(value.Id);
            writer.WriteString(value.Title);
            writer.WriteInt16((short)value.Status);
            writer.WriteNetworkConnection(value.Author);
        }

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