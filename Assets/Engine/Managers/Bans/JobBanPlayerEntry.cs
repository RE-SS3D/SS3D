using System;
using System.Collections.Generic;

[Serializable]
public class JobBanPlayerEntry
{
    public string userId;
    public string userName;
    public string ipAddress;
    public string clientId;

    public List<JobBanEntry> jobBanEntry = new List<JobBanEntry>();

    public JobBanEntry CheckForSpecificJob(JobType jobType)
    {
        var index = jobBanEntry.FindIndex(x => x.job == jobType);
        if (index != -1)
        {
            return jobBanEntry[index];
        }

        return null;
    }
}