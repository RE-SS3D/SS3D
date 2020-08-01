using System;

[Serializable]
public class BanEntry
{
    public string userId;
    public string userName;
    public double minutes;
    public string dateTimeOfBan;
    public string reason;
    public string ipAddress;
    public string clientId;

    public override string ToString()
    {
        return ("BanEntry of " + userId + " / " + userName + "\n"
                + "for " + minutes + " minutes, on" + dateTimeOfBan + "\n"
                + "on IP " + ipAddress + "\n"
                + "clientId " + clientId + "\n"
                + "for reason" + reason);
    }
}