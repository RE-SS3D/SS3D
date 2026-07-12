using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class IdConsoleInterfaceSnapshotSerializer
    {
        public static void WriteIdConsoleInterfaceSnapshot(this Writer writer, IdConsoleInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.Title);
            writer.WriteString(snapshot.Subtitle);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.PromptText);
            writer.WriteBoolean(snapshot.EditorUnlocked);
            writer.WriteBoolean(snapshot.HasTargetCard);
            writer.WriteString(snapshot.TargetName);
            writer.WriteString(snapshot.TargetJob);
            writer.WriteUInt64(snapshot.TargetAccessMask);

            int logCount = snapshot.LogEntryCount;
            if (logCount > IdConsoleInterfaceSnapshot.MaxLogEntries)
            {
                logCount = IdConsoleInterfaceSnapshot.MaxLogEntries;
            }

            writer.WriteByte((byte)logCount);
            for (int i = 0; i < logCount; i++)
            {
                writer.WriteString(GetLogEntry(snapshot, i));
            }
        }

        public static IdConsoleInterfaceSnapshot ReadIdConsoleInterfaceSnapshot(this Reader reader)
        {
            IdConsoleInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                Subtitle = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                PromptText = reader.ReadString(),
                EditorUnlocked = reader.ReadBoolean(),
                HasTargetCard = reader.ReadBoolean(),
                TargetName = reader.ReadString(),
                TargetJob = reader.ReadString(),
                TargetAccessMask = reader.ReadUInt64(),
                LogEntryCount = reader.ReadByte(),
            };

            if (snapshot.LogEntryCount > IdConsoleInterfaceSnapshot.MaxLogEntries)
            {
                snapshot.LogEntryCount = IdConsoleInterfaceSnapshot.MaxLogEntries;
            }

            for (int i = 0; i < snapshot.LogEntryCount; i++)
            {
                string logEntry = reader.ReadString();
                SetLogEntry(ref snapshot, i, logEntry);
            }

            return snapshot;
        }

        private static string GetLogEntry(IdConsoleInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Log0,
                1 => snapshot.Log1,
                2 => snapshot.Log2,
                3 => snapshot.Log3,
                _ => string.Empty,
            };
        }

        private static void SetLogEntry(ref IdConsoleInterfaceSnapshot snapshot, int index, string value)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Log0 = value;
                    break;
                }

                case 1:
                {
                    snapshot.Log1 = value;
                    break;
                }

                case 2:
                {
                    snapshot.Log2 = value;
                    break;
                }

                case 3:
                {
                    snapshot.Log3 = value;
                    break;
                }
            }
        }
    }
}
