using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SS3D.Launcher
{
    /// <summary>One remembered server. The admin token is stored in plain text on this computer only.</summary>
    [Serializable]
    public sealed class RecentServer
    {
        public string Name = "";
        public string Host = "";
        public int Port;
        public string AdminToken = "";
        public string Refusal = "";
        public string RefusalTime = "";

        public string Id => $"{Host}:{Port}";
        public string Address => MainMenuAddress.Format(Host, (ushort)Port);
    }

    /// <summary>
    /// One join from the menu, kept for its Retry. A server joins the recent list only once its login is
    /// accepted, so a first-time address has no row until then and its token waits here.
    /// </summary>
    public sealed class MainMenuJoinAttempt
    {
        public string Host { get; }
        public ushort Port { get; }
        public string Name { get; }
        public string Token { get; private set; }

        public string Id => $"{Host}:{Port}";

        public MainMenuJoinAttempt(string host, ushort port, string name, string token)
        {
            Host = host;
            Port = port;
            Name = name;
            Token = token ?? "";
        }

        /// <summary>Saves the reason on the server's row. False when the server has no row yet.</summary>
        public bool RecordRefusal(MainMenuState state, string reason, string time)
        {
            RecentServer server = state.Find(Id);
            if (server == null)
            {
                return false;
            }

            server.Refusal = reason;
            server.RefusalTime = time;
            return true;
        }

        /// <summary>Takes a token typed into the dialog. True when it went on a row that needs saving.</summary>
        public bool SetToken(MainMenuState state, string token)
        {
            Token = token ?? "";
            RecentServer server = state.Find(Id);
            if (server == null)
            {
                return false;
            }

            server.AdminToken = Token;
            return true;
        }

        /// <summary>The login was accepted: remember the server, with the token that got in.</summary>
        public RecentServer Complete(MainMenuState state, string reportedName)
        {
            RecentServer server = state.Remember(Host, Port, reportedName);
            if (Token.Length > 0)
            {
                server.AdminToken = Token;
            }

            return server;
        }
    }

    /// <summary>What the main menu remembers between launches (<see cref="MainMenuStore"/>).</summary>
    [Serializable]
    public sealed class MainMenuState
    {
        public const int MaxRecent = 5;

        public string PlayerName = "";
        public int LocalPort;
        /// <summary>Menu music on/off; its volume is the player's Music volume.</summary>
        public bool MusicOn = true;
        public List<RecentServer> Recent = new();

        public RecentServer Find(string id) => Recent.Find(server => server.Id == id);

        /// <summary>Moves (or adds) the server to the top after a successful join; keeps the newest five.</summary>
        public RecentServer Remember(string host, ushort port, string name)
        {
            string id = $"{host}:{port}";
            RecentServer server = Find(id) ?? new RecentServer { Host = host, Port = port };
            Recent.Remove(server);
            Recent.Insert(0, server);
            if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(server.Name))
            {
                server.Name = name.Trim();
            }

            server.Refusal = "";
            server.RefusalTime = "";
            if (Recent.Count > MaxRecent)
            {
                Recent.RemoveRange(MaxRecent, Recent.Count - MaxRecent);
            }

            return server;
        }

        public void Sanitize(ushort defaultPort)
        {
            PlayerName = (PlayerName ?? "").Trim();
            if (LocalPort is < 1 or > ushort.MaxValue)
            {
                LocalPort = defaultPort;
            }

            Recent ??= new List<RecentServer>();
            Recent.RemoveAll(server => server == null || string.IsNullOrWhiteSpace(server.Host)
                || server.Port is < 1 or > ushort.MaxValue);
            HashSet<string> seen = new();
            Recent.RemoveAll(server => !seen.Add(server.Id));
            if (Recent.Count > MaxRecent)
            {
                Recent.RemoveRange(MaxRecent, Recent.Count - MaxRecent);
            }

            foreach (RecentServer server in Recent)
            {
                server.Name ??= "";
                server.AdminToken ??= "";
                server.Refusal ??= "";
                server.RefusalTime ??= "";
            }
        }
    }

    /// <summary>Client-local JSON beside the player settings file.</summary>
    public sealed class MainMenuStore
    {
        private readonly string _path;

        public MainMenuStore(string path) => _path = path;

        public static string DefaultPath =>
            Path.Combine(UnityEngine.Application.persistentDataPath, "Settings", "main-menu.json");

        /// <summary>False when nothing was saved yet (first launch). Never returns a null state.</summary>
        public bool TryLoad(ushort defaultPort, out MainMenuState state)
        {
            state = new MainMenuState();
            bool loaded = false;
            if (File.Exists(_path))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(_path), state);
                    loaded = true;
                }
                catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
                {
                    Debug.LogWarning($"[MainMenu] Could not read {_path}; starting fresh: {exception.Message}");
                    state = new MainMenuState();
                }
            }

            state.Sanitize(defaultPort);
            return loaded;
        }

        public void Save(MainMenuState state)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
                string temporary = _path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(state, true));
                if (File.Exists(_path))
                {
                    File.Replace(temporary, _path, null);
                }
                else
                {
                    File.Move(temporary, _path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[MainMenu] Could not save {_path}: {exception.Message}");
            }
        }
    }
}
