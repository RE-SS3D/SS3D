using System;
using System.Text;
using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Networking.Settings;
using UnityEngine;

namespace SS3D.Systems.Rounds
{
    /// <summary>Admin-authored server identity shown on the lobby Server Info tab.</summary>
    [Serializable]
    public sealed class SavedServerInfo
    {
        public string serverName;
        public string motd;
    }

    /// <summary>Pure validation for server name / MOTD text (EditMode-tested).</summary>
    public static class ServerInfoText
    {
        public const int MaxNameLength = 64;
        public const int MaxMotdLength = 1000;
        public const string DefaultName = "SS3D Server";
        public const string DefaultMotd = "Welcome aboard. This is a community-maintained pre-alpha build — expect things to break, and report what you find.";

        /// <summary>Single line, no control characters, trimmed and clamped; empty becomes the default.</summary>
        public static string SanitizeName(string name)
        {
            string result = Clean(name, MaxNameLength, allowNewlines: false);
            return result.Length == 0 ? DefaultName : result;
        }

        /// <summary>Keeps paragraph breaks (at most two in a row), strips other control characters; empty becomes the default.</summary>
        public static string SanitizeMotd(string motd)
        {
            string result = Clean(motd, MaxMotdLength, allowNewlines: true);
            return result.Length == 0 ? DefaultMotd : result;
        }

        private static string Clean(string text, int maxLength, bool allowNewlines)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            StringBuilder builder = new(Math.Min(text.Length, maxLength));
            int newlines = 0;
            foreach (char raw in text.Replace("\r\n", "\n").Replace('\r', '\n'))
            {
                char c = raw;
                if (c == '\n' && allowNewlines)
                {
                    if (builder.Length == 0 || ++newlines > 2) continue;
                    while (builder.Length > 0 && builder[^1] == ' ') builder.Length--;
                    builder.Append('\n');
                    continue;
                }

                if (char.IsControl(c) || char.IsWhiteSpace(c)) c = ' ';
                if (c == ' ' && (builder.Length == 0 || builder[^1] == ' ' || builder[^1] == '\n')) continue;
                newlines = 0;
                builder.Append(c);
                if (builder.Length >= maxLength) break;
            }

            return builder.ToString().Trim();
        }
    }

    public sealed partial class RoundSubSystem
    {
        // Public lobby metadata. Name/MOTD persist under ServerMeta; the rest describe this server process.
        [SyncVar] private string _serverName = ServerInfoText.DefaultName;
        [SyncVar] private string _motd = ServerInfoText.DefaultMotd;
        [SyncVar] private string _publicMode = ServerSettings.Default.Mode;
        [SyncVar] private string _serverBuild = "";
        [SyncVar] private long _serverStartedUnix;

        public string ServerName => _serverName;
        public string Motd => _motd;
        /// <summary>The configured mode (Secret stays secret: it only resolves at round start).</summary>
        public string PublicMode => _publicMode;
        public string ServerBuild => _serverBuild;
        public DateTimeOffset ServerStarted => DateTimeOffset.FromUnixTimeSeconds(_serverStartedUnix);

        public override void OnStartServer()
        {
            base.OnStartServer();
            SavedServerInfo saved = LocalStorage.LoadObject<SavedServerInfo>(PersistencePaths.ServerInfo);
            _serverSettings.ServerName = ServerInfoText.SanitizeName(saved?.serverName);
            _serverSettings.Motd = ServerInfoText.SanitizeMotd(saved?.motd);
            if (ScriptableSettings.GetOrFind<NetworkSettings>().NetworkType == NetworkType.DedicatedServer)
                _serverSettings = ServerSettings.ForDedicatedServer(_serverSettings);
            if (SandboxProfile.RequestedOnCommandLine)
            {
                // In memory only: the saved server info and settings stay as they are.
                _serverSettings = SandboxProfile.Apply(_serverSettings);
                _allowLateJoin = _serverSettings.AllowLateJoin;
                Log.Information(this, "Sandbox profile applied", Logs.ServerOnly);
            }
            ApplyServerRules();
            StartAutoStartLoop();
            StartServerLifetime();
            RegisterBugReportBroadcasts();
            _serverName = _serverSettings.ServerName;
            _motd = _serverSettings.Motd;
            _publicMode = _serverSettings.Mode;
            _serverBuild = BuildInfo.Label;
            _serverStartedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>Name/MOTD are not round configuration: admins may edit them in any round state.</summary>
        [Server]
        private void SaveServerInfo(ServerSetting field, ServerSettings value)
        {
            if (field == ServerSetting.ServerName) _serverSettings.ServerName = ServerInfoText.SanitizeName(value.ServerName);
            else _serverSettings.Motd = ServerInfoText.SanitizeMotd(value.Motd);
            _serverName = _serverSettings.ServerName;
            _motd = _serverSettings.Motd;
            bool saved = LocalStorage.SaveObject(PersistencePaths.ServerInfo,
                new SavedServerInfo { serverName = _serverSettings.ServerName, motd = _serverSettings.Motd }, overwrite: true);
            if (!saved) Debug.LogWarning("[Rounds] Could not save server info; the change applies to this session only.");
        }
    }
}
