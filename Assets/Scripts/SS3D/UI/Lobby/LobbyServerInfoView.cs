using System;
using System.Globalization;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The lobby's Server Info tab (lobby.md §8): banner art, message of the day, the server's map, mode, build and
    /// uptime, and the client build's change log grouped by release. Built once; the setters change text in place.
    /// </summary>
    public sealed class LobbyServerInfoView
    {
        /// <summary>The five framed banners in <c>Lobby.uss</c> (<c>lobby-info__banner--1</c> … <c>--5</c>).</summary>
        public const int BannerCount = 5;

        /// <summary>The banners are 1920 × 750.</summary>
        private const float BannerAspect = 1920f / 750f;

        private readonly VisualElement _banner;
        private readonly Label _motd;
        private readonly Label _map;
        private readonly Label _mode;
        private readonly Label _build;
        private readonly Label _uptime;
        private DateTimeOffset? _started;
        private int _bannerIndex = -1;

        public VisualElement Root { get; }

        public LobbyServerInfoView()
        {
            Root = LobbyUi.Box("lobby-info");
            Root.name = "server-info";

            VisualElement left = LobbyUi.Box("lobby-info__left");
            _banner = LobbyUi.Box("lobby-info__banner");
            _banner.name = "server-info-banner";
            _banner.pickingMode = PickingMode.Ignore;
            _banner.RegisterCallback<GeometryChangedEvent>(e =>
            {
                float height = Mathf.Round(e.newRect.width / BannerAspect);
                if (Mathf.Abs(_banner.resolvedStyle.height - height) > 0.5f) _banner.style.height = height;
            });
            left.Add(_banner);
            left.Add(LobbyUi.Text("MESSAGE OF THE DAY", "lobby__eyebrow"));
            ScrollView motdScroll = new(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            motdScroll.AddToClassList("lobby-info__motd-scroll");
            _motd = LobbyUi.Text("", "lobby-info__motd");
            _motd.name = "server-info-motd";
            motdScroll.Add(_motd);
            left.Add(motdScroll);
            Root.Add(left);

            VisualElement right = LobbyUi.Box("lobby-info__right");
            right.Add(LobbyUi.Text("SERVER", "lobby__eyebrow"));
            _map = AddRow(right, "Map", "server-info-map", false);
            _mode = AddRow(right, "Mode", "server-info-mode", false);
            _build = AddRow(right, "Build", "server-info-build", true);
            _uptime = AddRow(right, "Uptime", "server-info-uptime", true);
            right.Add(BuildChangelog());
            Root.Add(right);
        }

        private static Label AddRow(VisualElement parent, string key, string name, bool mono)
        {
            VisualElement row = LobbyUi.Box("lobby-info__kv");
            row.Add(LobbyUi.Text(key, "lobby-info__key"));
            Label value = mono ? LobbyUi.Mono("—", "lobby-info__value") : LobbyUi.Text("—", "lobby-info__value");
            value.name = name;
            row.Add(value);
            parent.Add(row);
            return value;
        }

        public void SetServer(string motd, string mode, string build, DateTimeOffset? started)
        {
            _motd.text = motd ?? "";
            _mode.text = string.IsNullOrEmpty(mode) ? "—" : mode;
            _build.text = string.IsNullOrEmpty(build) ? "—" : build;
            _started = started;
            RefreshUptime();
        }

        public void SetMap(string stationName) => _map.text = string.IsNullOrEmpty(stationName) ? "No map loaded" : stationName;

        /// <summary>One banner per round, the same on every client (decoration only, never information).</summary>
        public void SetRound(int roundId)
        {
            int index = (roundId % BannerCount + BannerCount) % BannerCount + 1;
            if (index == _bannerIndex) return;
            if (_bannerIndex > 0) _banner.RemoveFromClassList("lobby-info__banner--" + _bannerIndex);
            _bannerIndex = index;
            _banner.AddToClassList("lobby-info__banner--" + index);
        }

        public void RefreshUptime() => _uptime.text = Uptime(_started, DateTimeOffset.UtcNow);

        public static string Uptime(DateTimeOffset? started, DateTimeOffset now)
        {
            if (started is not { } start || start.ToUnixTimeSeconds() <= 0) return "—";
            TimeSpan up = now - start;
            if (up < TimeSpan.Zero) up = TimeSpan.Zero;
            return up.TotalDays >= 1
                ? $"{(int)up.TotalDays}d {up.Hours:00}:{up.Minutes:00}"
                : $"{(int)up.TotalHours:00}:{up.Minutes:00}";
        }

        /// <summary>
        /// The client build's change log (<see cref="BuildInfo"/>): merged PR titles under the release that shipped
        /// them, newest first. Rows open the PR; each release links its notes.
        /// </summary>
        private static VisualElement BuildChangelog()
        {
            BuildInfo.Data info = BuildInfo.Current;
            VisualElement column = LobbyUi.Box("lobby-info__section-gap");
            column.style.flexGrow = 1;
            column.style.flexShrink = 1;
            column.style.minHeight = 0;

            VisualElement header = LobbyUi.Box("lobby-info__links");
            header.Add(LobbyUi.Text("CHANGE LOG", "lobby__eyebrow"));
            if (info.ReleasesUrl is { } releases) header.Add(Link("All releases", releases, "changelog-releases-link"));
            column.Add(header);

            ScrollView log = new(ScrollViewMode.Vertical) { name = "changelog-list" };
            log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            log.AddToClassList("lobby-info__log");
            column.Add(log);

            VisualElement group = null;
            string currentRelease = null;
            foreach (BuildInfo.Entry entry in info.entries)
            {
                string release = entry.release ?? string.Empty;
                if (group == null || release != currentRelease)
                {
                    currentRelease = release;
                    group = LobbyUi.Box("lobby-info__release");
                    VisualElement head = LobbyUi.Box("lobby-info__release-head");
                    head.Add(LobbyUi.Mono(release.Length > 0 ? $"v{release}" : "In development", "lobby-info__release-version"));
                    head.Add(LobbyUi.Text(ShortDate(entry.date), "lobby-info__release-date"));
                    if (release.Length > 0 && info.ReleasesUrl is { } url)
                        head.Add(Link("Release notes", $"{url}/tag/v{release}", null));
                    group.Add(head);
                    log.Add(group);
                }

                VisualElement row = LobbyUi.Box("lobby-info__entry");
                row.tooltip = entry.date;
                VisualElement bullet = LobbyUi.Box("lobby-info__bullet");
                bullet.pickingMode = PickingMode.Ignore;
                row.Add(bullet);
                row.Add(LobbyUi.Text(entry.subject, "lobby-info__entry-text"));
                row.Add(LobbyUi.Mono(entry.pr > 0 ? $"#{entry.pr}" : entry.hash, "lobby-info__entry-ref"));
                if (entry.Url(info.repository) is { } entryUrl)
                {
                    row.AddToClassList("lobby-info__entry--link");
                    row.RegisterCallback<ClickEvent>(_ => Application.OpenURL(entryUrl));
                }

                group.Add(row);
            }

            if (info.entries.Length == 0) log.Add(LobbyUi.Text("No change log in this build.", "lobby-info__empty"));
            return column;
        }

        private static string ShortDate(string date)
        {
            if (string.IsNullOrEmpty(date)) return "";
            return DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)
                ? parsed.ToString("d MMM yyyy", CultureInfo.InvariantCulture) : date;
        }

        private static VisualElement Link(string text, string url, string name)
        {
            VisualElement link = LobbyUi.Box("lobby-info__link");
            if (name != null) link.name = name;
            link.tooltip = url;
            link.Add(LobbyUi.Text(text, "lobby-info__link-label"));
            link.Add(LobbyUi.Icon("external"));
            link.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
            return link;
        }
    }
}
