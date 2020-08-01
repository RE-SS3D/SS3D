using System;

[Serializable]
public class JobBanEntry
{
    public JobType job;
    public bool isPerma;
    public double minutes;
    public string dateTimeOfBan;
    public string reason;
}