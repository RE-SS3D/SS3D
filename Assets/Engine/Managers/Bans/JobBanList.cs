using System;
using System.Collections.Generic;

[Serializable]
public class JobBanList
{
    public List<JobBanPlayerEntry> jobBanEntries = new List<JobBanPlayerEntry>();

    public (JobBanPlayerEntry, int) CheckForEntry(string userId, string ipAddress, string clientId)
    {
        var index = jobBanEntries.FindIndex(x => x.userId == userId
                                                 || x.ipAddress == ipAddress
                                                 || x.clientId == clientId);
        if (index != -1)
        {
            return (jobBanEntries[index], index);
        }

        return (null, 0);
    }
}