using SS3D.Systems.IdAccess;

namespace SS3D.UI.MachineInterface
{
    public static class IdConsoleInterfaceSnapshotMapper
    {
        public static IdConsoleInterfaceViewModel ToViewModel(IdConsoleInterfaceSnapshot snapshot)
        {
            IdConsoleInterfaceViewModel model = new()
            {
                Title = snapshot.Title,
                Subtitle = snapshot.Subtitle,
                ModelLabel = snapshot.ModelLabel,
                PromptText = snapshot.PromptText,
                EditorUnlocked = snapshot.EditorUnlocked,
                HasTargetCard = snapshot.HasTargetCard,
                TargetName = snapshot.TargetName,
                TargetJob = snapshot.TargetJob,
            };

            AccessMask targetAccess = new(snapshot.TargetAccessMask);
            foreach (AccessLevelEntry entry in AccessLevelCatalog.AllEditableLevels)
            {
                model.Levels.Add(new IdConsoleAccessLevelViewData
                {
                    Index = (byte)model.Levels.Count,
                    Name = entry.DisplayName,
                    IsDepartment = entry.IsDepartment,
                    Enabled = targetAccess.HasAll(new AccessMask(entry.Level)),
                });
            }

            int logCount = snapshot.LogEntryCount;
            if (logCount > IdConsoleInterfaceSnapshot.MaxLogEntries)
            {
                logCount = IdConsoleInterfaceSnapshot.MaxLogEntries;
            }

            for (int i = 0; i < logCount; i++)
            {
                model.EditLog.Add(GetLogEntry(snapshot, i));
            }

            return model;
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
    }
}
