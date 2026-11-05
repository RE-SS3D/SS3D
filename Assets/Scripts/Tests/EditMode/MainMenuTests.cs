using System.IO;
using System.Net;
using LiteNetLib;
using NUnit.Framework;
using SS3D.Launcher;
using SS3D.Networking.Status;
using SS3D.UI.Shell.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace SS3D.Tests.EditMode
{
    public sealed class MainMenuTests
    {
        [Test]
        public void WallpaperPick_CoversStillsThenVideos_AndEveryVideoHasAFallbackStill()
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            bool seenVideo = false;
            for (int index = 0; index < catalog.WallpaperCount; index++)
            {
                Assert.IsNotNull(catalog.Wallpaper(index), $"index {index} has no still");
                bool isVideo = catalog.WallpaperVideo(index) != null;
                Assert.IsFalse(seenVideo && !isVideo, $"stills come first (index {index})");
                seenVideo |= isVideo;
            }

            Assert.IsTrue(seenVideo, "no videos in the catalog; run Rebuild All UI Catalogs");
            Assert.IsNull(catalog.WallpaperVideo(catalog.WallpaperCount));
        }

        [Test]
        public void PickWallpaper_SplitsLaunchesBetweenVideosAndStills_AndStaysInRange()
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            Assert.IsNotNull(catalog.WallpaperVideo(catalog.PickWallpaper(0.49f, 0f)), "low roll is a video");
            Assert.IsNotNull(catalog.WallpaperVideo(catalog.PickWallpaper(0.49f, 1f)), "top of the video range");
            Assert.IsNull(catalog.WallpaperVideo(catalog.PickWallpaper(0.5f, 0f)), "high roll is a still");
            Assert.IsNull(catalog.WallpaperVideo(catalog.PickWallpaper(1f, 1f)), "top of the still range");
            Assert.AreEqual(catalog.WallpaperCount - 1, catalog.PickWallpaper(0f, 1f), "last video");
            Assert.AreNotEqual(catalog.PickWallpaper(1f, 0f), catalog.PickWallpaper(1f, 0.9f), "item roll chooses");
        }

        [Test]
        public void PickOtherWallpaper_NeverRepeatsThePrevious()
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            int previous = catalog.PickWallpaper(0f, 0f);
            Assert.AreNotEqual(previous, MainMenuSubSystem.PickOtherWallpaper(catalog, previous, () => 0f));
            float[] rolls = { 0f, 0f, 0f, 0f, 1f, 0f };
            int roll = 0;
            Assert.AreEqual(catalog.PickWallpaper(1f, 0f), MainMenuSubSystem.PickOtherWallpaper(catalog, previous, () => rolls[roll++]), "rerolls past a repeat");
        }

        [Test]
        public void Rotation_HoldsThenDipsSwapsAndLifts()
        {
            MainMenuWallpaperRotation rotation = new(0f);
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(59f, true, true));
            Assert.AreEqual(MainMenuWallpaperStep.FadeOut, rotation.Tick(60f, true, true));
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(60.2f, true, true), "still fading");
            Assert.AreEqual(MainMenuWallpaperStep.Swap, rotation.Tick(60.5f, true, false));
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(60.6f, true, false), "video has no frame yet");
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(60.7f, true, true), "video layer still crossfading");
            Assert.AreEqual(MainMenuWallpaperStep.FadeIn, rotation.Tick(61.2f, true, true));
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(120f, true, true), "a new hold starts at the lift");
            Assert.AreEqual(MainMenuWallpaperStep.FadeOut, rotation.Tick(121.2f, true, true));
        }

        [Test]
        public void Rotation_LiftsTheVeilWhenAVideoNeverLoads()
        {
            MainMenuWallpaperRotation rotation = new(0f);
            rotation.Tick(60f, true, false);
            rotation.Tick(60.5f, true, false);
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(61.9f, true, false));
            Assert.AreEqual(MainMenuWallpaperStep.FadeIn, rotation.Tick(62f, true, false));
        }

        [Test]
        public void Rotation_RevealWaitsForTheVideoThenStartsAFullHold()
        {
            MainMenuWallpaperRotation rotation = new(0f);
            rotation.Reveal(0f);
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(0.1f, true, false), "no frame yet");
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(0.2f, true, true), "video layer crossfading");
            Assert.AreEqual(MainMenuWallpaperStep.FadeIn, rotation.Tick(0.7f, true, true));
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(60f, true, true));
            Assert.AreEqual(MainMenuWallpaperStep.FadeOut, rotation.Tick(60.7f, true, true));
        }

        [Test]
        public void Rotation_WaitsOutDialogs_AndLiftsIfOneOpensMidDip()
        {
            MainMenuWallpaperRotation rotation = new(0f);
            Assert.AreEqual(MainMenuWallpaperStep.None, rotation.Tick(300f, false, true), "no dip behind a dialog");
            Assert.AreEqual(MainMenuWallpaperStep.FadeOut, rotation.Tick(301f, true, true), "due as soon as it closes");
            Assert.AreEqual(MainMenuWallpaperStep.Swap, rotation.Tick(301.5f, false, true), "the swap still lands");
            Assert.AreEqual(MainMenuWallpaperStep.FadeIn, rotation.Tick(301.6f, false, true), "no black behind the dialog");
        }

        [Test]
        public void Wallpapers_Are1080pAndCompressed()
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            for (int index = 0; index < catalog.WallpaperCount && catalog.WallpaperVideo(index) == null; index++)
            {
                Texture2D wallpaper = catalog.Wallpaper(index);
                string path = AssetDatabase.GetAssetPath(wallpaper);
                Assert.AreEqual(new Vector2Int(1920, 1080), new Vector2Int(wallpaper.width, wallpaper.height), path);
                Assert.AreEqual(TextureImporterCompression.CompressedHQ, ((TextureImporter)AssetImporter.GetAtPath(path)).textureCompression, path);
            }
        }

        /// <summary>VP8 is the one codec Unity decodes itself on every target; H.264 would rely on the OS.</summary>
        [Test]
        public void WallpaperVideos_AreSilentUntranscodedVp8()
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            for (int index = 0; index < catalog.WallpaperCount; index++)
            {
                if (catalog.WallpaperVideo(index) is not { } video) continue;
                string path = AssetDatabase.GetAssetPath(video);
                StringAssert.EndsWith(".webm", path);
                Assert.AreEqual((ushort)0, video.audioTrackCount, $"{path}: audio would fight the menu music");
                Assert.IsFalse(((VideoClipImporter)AssetImporter.GetAtPath(path)).defaultTargetSettings.enableTranscoding, path);
            }
        }

        [Test]
        public void StatusReply_RoundTrips_AndRejectsOtherPackets()
        {
            byte[] reply = ServerStatusPacket.EncodeReply(new ServerStatus(9, "0.5.0", "Sandbox", 3, 16));
            Assert.IsTrue(ServerStatusPacket.TryDecodeReply(reply, out ServerStatus status));
            Assert.AreEqual(9, status.Protocol);
            Assert.AreEqual("0.5.0", status.Version);
            Assert.AreEqual("Sandbox", status.Name);
            Assert.AreEqual(3, status.Players);
            Assert.AreEqual(16, status.PlayerCap);

            Assert.IsTrue(ServerStatusPacket.IsRequest(ServerStatusPacket.Request()));
            Assert.IsFalse(ServerStatusPacket.IsRequest(reply));
            Assert.IsFalse(ServerStatusPacket.TryDecodeReply(ServerStatusPacket.Request(), out _));
            Assert.IsFalse(ServerStatusPacket.TryDecodeReply(reply[..^3], out _), "truncated reply");
            Assert.IsFalse(ServerStatusPacket.TryDecodeReply(null, out _));
        }

        [Test]
        public void StatusReply_ClipsLongNames()
        {
            byte[] reply = ServerStatusPacket.EncodeReply(new ServerStatus(1, "1", new string('x', 500), 0, 0));
            Assert.IsTrue(ServerStatusPacket.TryDecodeReply(reply, out ServerStatus status));
            Assert.AreEqual(64, status.Name.Length);
        }

        [Test]
        public void Classify_ComparesProtocolAndCapacity()
        {
            const int local = 9;
            Assert.AreEqual(MainMenuServerState.Checking, MainMenuServers.Classify(null, local));
            Assert.AreEqual(MainMenuServerState.Offline, MainMenuServers.Classify(ServerStatusResult.Unreachable, local));
            Assert.AreEqual(MainMenuServerState.NeedsUpdate, MainMenuServers.Classify(Reply(10, 1, 16), local));
            Assert.AreEqual(MainMenuServerState.OlderVersion, MainMenuServers.Classify(Reply(8, 1, 16), local));
            Assert.AreEqual(MainMenuServerState.Full, MainMenuServers.Classify(Reply(9, 16, 16), local));
            Assert.AreEqual(MainMenuServerState.Compatible, MainMenuServers.Classify(Reply(9, 15, 16), local));
        }

        [Test]
        public void ServerRow_PrefersOwnLabel_ThenReportedName_ThenAddress()
        {
            RecentServer server = new() { Host = "sandbox.example", Port = 1151 };
            Assert.AreEqual("sandbox.example:1151", MainMenuServers.Present(server, null, false, 9).Name);
            MainMenuServerPresentation row = MainMenuServers.Present(server, Reply(9, 2, 16), false, 9);
            Assert.AreEqual("Sandbox", row.Name);
            Assert.AreEqual("2/16", row.Players);
            Assert.AreEqual("40 ms", row.Ping);
            server.Name = "My box";
            Assert.AreEqual("My box", MainMenuServers.Present(server, Reply(9, 2, 16), false, 9).Name);
        }

        [TestCase("sandbox.example", "sandbox.example", 1151)]
        [TestCase(" 10.0.0.5:7777 ", "10.0.0.5", 7777)]
        [TestCase("[::1]:7000", "::1", 7000)]
        [TestCase("::1", "::1", 1151)]
        public void Address_ParsesHostAndPort(string input, string host, int port)
        {
            Assert.IsTrue(MainMenuAddress.TryParse(input, 1151, out string parsedHost, out ushort parsedPort, out string error), error);
            Assert.AreEqual(host, parsedHost);
            Assert.AreEqual(port, parsedPort);
        }

        [TestCase("")]
        [TestCase("host:0")]
        [TestCase("host:70000")]
        [TestCase("host:port")]
        [TestCase(":7777")]
        [TestCase("https://example.org")]
        [TestCase("[::1")]
        public void Address_RejectsInvalidInput(string input)
        {
            Assert.IsFalse(MainMenuAddress.TryParse(input, 1151, out _, out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Remember_MovesToTop_KeepsFive_AndClearsRefusal()
        {
            MainMenuState state = new();
            for (int i = 0; i < 6; i++)
            {
                state.Remember($"host{i}", 1151, $"Server {i}");
            }

            Assert.AreEqual(MainMenuState.MaxRecent, state.Recent.Count);
            Assert.AreEqual("host5", state.Recent[0].Host);
            Assert.IsNull(state.Find("host0:1151"), "oldest dropped");

            state.Find("host2:1151").Refusal = "Version mismatch";
            state.Find("host2:1151").AdminToken = "secret";
            state.Find("host2:1151").Name = "Renamed";
            RecentServer again = state.Remember("host2", 1151, "Reported");
            Assert.AreSame(again, state.Recent[0]);
            Assert.AreEqual("", again.Refusal);
            Assert.AreEqual("secret", again.AdminToken);
            Assert.AreEqual("Renamed", again.Name, "the player's label wins over the reported name");
        }

        [Test]
        public void Store_RoundTrips_AndTreatsMissingOrBrokenFilesAsFirstLaunch()
        {
            string folder = Path.Combine(Path.GetTempPath(), "ss3d-main-menu-" + System.Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "main-menu.json");
            try
            {
                MainMenuStore store = new(path);
                Assert.IsFalse(store.TryLoad(1151, out MainMenuState fresh));
                Assert.AreEqual(1151, fresh.LocalPort);

                fresh.PlayerName = " Ada ";
                fresh.LocalPort = 7777;
                fresh.Remember("10.0.0.5", 7777, "Lab").AdminToken = "t0k";
                store.Save(fresh);
                Assert.IsTrue(store.TryLoad(1151, out MainMenuState loaded));
                Assert.AreEqual("Ada", loaded.PlayerName);
                Assert.AreEqual(7777, loaded.LocalPort);
                Assert.AreEqual("t0k", loaded.Find("10.0.0.5:7777").AdminToken);

                File.WriteAllText(path, "not json");
                LogAssertIgnoringWarning(() => Assert.IsFalse(store.TryLoad(1151, out _)));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ReleaseCheck_PicksNewestStableWithPlatformAsset()
        {
            const string json = "[" +
                "{\"tag_name\":\"v0.10.0\",\"draft\":false,\"prerelease\":true,\"assets\":[{\"name\":\"A-v0.10.0\"}]}," +
                "{\"tag_name\":\"develop-nightly\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"A-develop-nightly\"}]}," +
                "{\"tag_name\":\"v0.9.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"Other-v0.9.0\"}]}," +
                "{\"tag_name\":\"v0.8.1\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"A-v0.8.1\"}]}," +
                "{\"tag_name\":\"v0.8.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"A-v0.8.0\"}]}," +
                "{\"tag_name\":\"v9.0.0\",\"draft\":true,\"prerelease\":false,\"assets\":[{\"name\":\"A-v9.0.0\"}]}]";
            Assert.AreEqual("v0.8.1", ReleaseCheck.LatestStable(json, tag => "A-" + tag));
            Assert.IsNull(ReleaseCheck.LatestStable("{\"message\":\"rate limited\"}", tag => "A-" + tag));
            Assert.IsNull(ReleaseCheck.LatestStable("[]", tag => "A-" + tag));
        }

        [TestCase("v0.5.0", "0.4.9", true)]
        [TestCase("v0.10.0", "0.9.3", true)]
        [TestCase("v0.5.0", "0.5.0-dev.3+abc", true)]
        [TestCase("v0.5.0", "0.5.0", false)]
        [TestCase("v0.5.0", "0.5.1-dev.1+abc", false)]
        [TestCase("v0.5.0", "0.0.0-editor", false)]
        [TestCase("v0.5.0", "garbage", false)]
        public void ReleaseCheck_IsNewer(string tag, string local, bool expected)
        {
            Assert.AreEqual(expected, ReleaseCheck.IsNewer(tag, local));
        }

        [Test]
        public void StatusQuery_ReadsAReplyOverLoopback()
        {
            EventBasedNetListener listener = new();
            NetManager server = new(listener) { UnconnectedMessagesEnabled = true };
            byte[] reply = ServerStatusPacket.EncodeReply(new ServerStatus(9, "0.5.0", "Loopback", 1, 4));
            listener.NetworkReceiveUnconnectedEvent += (remote, reader, type) =>
            {
                if (ServerStatusPacket.IsRequest(reader.GetRemainingBytes())) server.SendUnconnectedMessage(reply, remote);
                reader.Recycle();
            };
            Assert.IsTrue(server.Start(IPAddress.Loopback, IPAddress.IPv6Loopback, 0));
            using ServerStatusQuery query = new();
            ServerStatusResult? result = null;
            query.Completed += (key, value) => result = value;
            try
            {
                query.Query("loopback", "127.0.0.1", (ushort)server.LocalPort);
                System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
                while (result == null && clock.Elapsed.TotalSeconds < 5)
                {
                    server.PollEvents();
                    query.Tick();
                    System.Threading.Thread.Sleep(10);
                }
            }
            finally
            {
                server.Stop();
            }

            Assert.IsTrue(result.HasValue, "no reply within 5 s");
            Assert.IsTrue(result.Value.Reachable);
            Assert.AreEqual("Loopback", result.Value.Status.Name);
        }

        [TestCase("GatewayToTheForest", "Gateway to the Forest")]
        [TestCase("Breach", "Breach")]
        [TestCase("Space Theme", "Space Theme")]
        [TestCase("Title1", "Title 1")]
        [TestCase("SpacemansLullaby", "Spaceman's Lullaby")]
        public void MusicTitle_IsReadableFromTheClipName(string clip, string title)
        {
            Assert.AreEqual(title, MainMenuMusic.TitleOf(clip));
        }

        private static ServerStatusResult Reply(int protocol, int players, int cap) =>
            new(true, new ServerStatus(protocol, "0.5.0", "Sandbox", players, cap), 40);

        private static void LogAssertIgnoringWarning(System.Action action)
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Could not read"));
            action();
        }
    }
}
