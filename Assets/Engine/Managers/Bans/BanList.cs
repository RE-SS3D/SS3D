using System;
using System.Collections.Generic;

[Serializable]
public class BanList
{
    public List<BanEntry> banEntries = new List<BanEntry>();

    public BanEntry CheckForEntry(string userId, string ipAddress, string clientId)
    {
        var index = banEntries.FindIndex(x => x.userId == userId
                                              || x.ipAddress == ipAddress
                                              || x.clientId == clientId);
        if (index != -1)
        {
            return banEntries[index];
        }

        return null;
    }
}