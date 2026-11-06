using System;
using System.Linq;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;

namespace SS3D.Networking
{
    /// <summary>Why the server ended a connection. The client renders the text, except a kick's message.</summary>
    public enum DisconnectCode : byte
    {
        None = 0,
        ServerFull = 1,
        AdminToken = 2,
        VersionMismatch = 3,
        ServerRestarting = 4,
        Kicked = 5,
    }

    /// <summary>Server → client, just before the server closes the connection.</summary>
    public struct DisconnectBroadcast : IBroadcast
    {
        public DisconnectCode Code;
        public string Text;
    }

    /// <summary>Server → client: the login passed, so this join is a real one.</summary>
    public struct LoginAcceptedBroadcast : IBroadcast
    {
    }

    /// <summary>
    /// Disconnect reasons and the login result. On the server, <see cref="Disconnect"/> is the one way
    /// to end a connection with a reason. On the client it holds the last reason received, cleared by
    /// the next connect attempt. No reason received means the connection was simply lost; FishNet's own
    /// kicks (a bad RPC) can't carry one and read that way.
    /// </summary>
    public static class ConnectionNotices
    {
        public const string Lost = "Connection to the server was lost.";

        /// <summary>How long the server waits for a told client to close the connection itself.</summary>
        public const float GraceSeconds = 2f;

        public static DisconnectCode LastCode { get; private set; }

        /// <summary>Raised on the client when the server accepts its login.</summary>
        public static event Action LoginAccepted;

        /// <summary>Whether the server accepted the login of this connection.</summary>
        public static bool LoginWasAccepted { get; private set; }

        private static string _lastText;

        public static bool HasReason => LastCode != DisconnectCode.None;

        /// <summary>What to show for the last reason received.</summary>
        public static string LastReason => Describe(LastCode, _lastText);

        public static string Describe(DisconnectCode code, string text)
        {
            switch (code)
            {
                case DisconnectCode.ServerFull:
                    return "The server is full.";
                case DisconnectCode.AdminToken:
                    return "This name is an admin name on this server. Enter its admin token.";
                case DisconnectCode.VersionMismatch:
                    return string.IsNullOrEmpty(text) ? "This server runs a different version." : text;
                case DisconnectCode.ServerRestarting:
                    return "The server is restarting.";
                case DisconnectCode.Kicked:
                    return string.IsNullOrEmpty(text) ? "You were kicked from the server." : $"You were kicked: {text}";
                default:
                    return Lost;
            }
        }

        /// <summary>
        /// Tells the client why and lets it close the connection, closing it from here only after
        /// <see cref="GraceSeconds"/>. A close from the server right behind the message loses it on any
        /// client frame long enough to poll both together, and a joining client's frames are: Tugboat
        /// reads the disconnect first and discards the queued packets.
        /// </summary>
        public static void Disconnect(NetworkConnection conn, DisconnectCode code, string text = null)
        {
            NetworkManager networkManager = conn.NetworkManager;
            if (conn.Disconnecting || !networkManager.TryGetComponent(out ProtocolVersionAuthenticator authenticator))
            {
                return;
            }

            networkManager.ServerManager.Broadcast(conn, new DisconnectBroadcast { Code = code, Text = text }, false);
            authenticator.CloseAfterGrace(conn);
        }

        public static void DisconnectAll(NetworkManager networkManager, DisconnectCode code, string text = null)
        {
            foreach (NetworkConnection conn in networkManager.ServerManager.Clients.Values.ToArray())
            {
                Disconnect(conn, code, text);
            }
        }

        public static void AcceptLogin(NetworkConnection conn) =>
            conn.NetworkManager.ServerManager.Broadcast(conn, new LoginAcceptedBroadcast(), false);

        public static void Clear()
        {
            LastCode = DisconnectCode.None;
            _lastText = null;
            LoginWasAccepted = false;
        }

        internal static void Record(DisconnectCode code, string text)
        {
            LastCode = code;
            _lastText = text;
        }

        internal static void RaiseLoginAccepted()
        {
            LoginWasAccepted = true;
            LoginAccepted?.Invoke();
        }
    }
}
