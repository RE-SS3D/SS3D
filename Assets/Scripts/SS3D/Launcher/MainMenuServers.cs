using SS3D.Networking.Status;
using SS3D.UI.Shell.MainMenu;

namespace SS3D.Launcher
{
    /// <summary>Turns a remembered server and its latest status reply into a server-list row.</summary>
    public static class MainMenuServers
    {
        /// <param name="result">Latest reply, or null before the first check finished.</param>
        public static MainMenuServerPresentation Present(RecentServer server, ServerStatusResult? result, bool expanded, int localProtocol)
        {
            MainMenuServerPresentation row = new()
            {
                Id = server.Id,
                Address = server.Address,
                Refusal = server.Refusal,
                RefusalTime = server.RefusalTime,
                Expanded = expanded,
                AdminToken = server.AdminToken,
                State = Classify(result, localProtocol),
            };

            string reported = result is { Reachable: true } ? result.Value.Status.Name : "";
            row.Name = !string.IsNullOrWhiteSpace(server.Name) ? server.Name
                : !string.IsNullOrWhiteSpace(reported) ? reported
                : server.Address;
            if (result is { Reachable: true } reply)
            {
                row.RequiredVersion = reply.Status.Version;
                row.Players = reply.Status.PlayerCap > 0 ? $"{reply.Status.Players}/{reply.Status.PlayerCap}" : reply.Status.Players.ToString();
                row.Ping = $"{reply.PingMilliseconds} ms";
            }

            return row;
        }

        public static MainMenuServerState Classify(ServerStatusResult? result, int localProtocol)
        {
            if (result == null)
            {
                return MainMenuServerState.Checking;
            }

            if (!result.Value.Reachable)
            {
                return MainMenuServerState.Offline;
            }

            ServerStatus status = result.Value.Status;
            if (status.Protocol > localProtocol)
            {
                return MainMenuServerState.NeedsUpdate;
            }

            if (status.Protocol < localProtocol)
            {
                return MainMenuServerState.OlderVersion;
            }

            return status.IsFull ? MainMenuServerState.Full : MainMenuServerState.Compatible;
        }
    }
}
