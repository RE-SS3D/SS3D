using System.IO;
using System.Net;
using LiteNetLib;
using NUnit.Framework;
using SS3D.Launcher;
using SS3D.Networking;
using SS3D.Networking.Status;
using SS3D.UI.Shell.MainMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
        public void JoinAttempt_RefusalOnAFirstTimeAddress_AddsNoRow_AndKeepsTheToken()
        {
            MainMenuState state = new();
            MainMenuJoinAttempt join = new("new.example", 1151, "Direct connect", "");

            Assert.IsFalse(join.RecordRefusal(state, "refused", "21:00"));
            Assert.IsFalse(join.SetToken(state, "typed"));
            Assert.IsEmpty(state.Recent, "only an accepted login joins the list");
            Assert.AreEqual("typed", join.Token, "Retry sends it");

            RecentServer server = join.Complete(state, "Reported");
            Assert.AreEqual("typed", server.AdminToken, "written once the login is accepted");
            Assert.AreEqual("Reported", server.Name);
            Assert.AreSame(server, state.Recent[0]);
        }

        [Test]
        public void JoinAttempt_OnAKnownServer_SavesRefusalAndTokenToItsRow_AndSuccessClearsTheRefusal()
        {
            MainMenuState state = new();
            RecentServer server = state.Remember("known.example", 1151, "Known");
            MainMenuJoinAttempt join = new("known.example", 1151, "Known", "");

            Assert.IsTrue(join.RecordRefusal(state, "refused", "21:00"));
            Assert.AreEqual("refused", server.Refusal);
            Assert.AreEqual("21:00", server.RefusalTime);
            Assert.IsTrue(join.SetToken(state, "typed"));
            Assert.AreEqual("typed", server.AdminToken);

            join.Complete(state, "");
            Assert.AreEqual("", server.Refusal);
            Assert.AreEqual("typed", server.AdminToken);
        }

        [Test]
        public void JoinAttempt_WithNoToken_KeepsTheRowsToken()
        {
            MainMenuState state = new();
            state.Remember("known.example", 1151, "Known").AdminToken = "saved";
            new MainMenuJoinAttempt("known.example", 1151, "Known", "").Complete(state, "");
            Assert.AreEqual("saved", state.Recent[0].AdminToken);
        }

        [TestCase(DisconnectCode.ServerFull, null, "The server is full.")]
        [TestCase(DisconnectCode.AdminToken, null, "This name is an admin name on this server. Enter its admin token.")]
        [TestCase(DisconnectCode.ServerRestarting, null, "The server is restarting.")]
        [TestCase(DisconnectCode.Kicked, null, "You were kicked from the server.")]
        [TestCase(DisconnectCode.Kicked, "griefing", "You were kicked: griefing")]
        [TestCase(DisconnectCode.VersionMismatch, "server is 1.0", "server is 1.0")]
        [TestCase(DisconnectCode.None, null, ConnectionNotices.Lost)]
        public void DisconnectCode_RendersOnTheClient(DisconnectCode code, string text, string expected)
        {
            Assert.AreEqual(expected, ConnectionNotices.Describe(code, text));
        }

        private static MainMenuView DialogView(MainMenuPresentation model)
        {
            MainMenuAssetCatalog catalog = Resources.Load<MainMenuAssetCatalog>(MainMenuAssetPaths.ResourcesCatalogName);
            MainMenuView view = new(catalog);
            model.Screen = MainMenuScreen.Connecting;
            view.Present(model);
            return view;
        }

        private static bool Shown(MainMenuView view, string name) =>
            !view.Root.Q(name).ClassListContains("main-menu__hidden");

        [Test]
        public void Dialog_WhileJoining_OffersCancelOnly_AndCountsTheLoginStep()
        {
            MainMenuView view = DialogView(new MainMenuPresentation { OperationStep = 3 });
            Assert.IsTrue(Shown(view, "cancel"));
            Assert.IsFalse(Shown(view, "retry"));
            Assert.IsFalse(Shown(view, "back-to-servers"));
            Assert.IsFalse(Shown(view, "overlay-token-row"));
            Assert.AreEqual(4, view.Root.Q("progress-steps").childCount);
            Assert.AreEqual("CONNECTING", view.Root.Q<Label>("overlay-title").text);
        }

        [Test]
        public void Dialog_OnATokenRefusal_MarksTheStep_AndOffersRetryBackAndToken()
        {
            MainMenuView view = DialogView(new MainMenuPresentation
            {
                OperationStep = 3, OperationFailed = true, OperationNeedsToken = true,
                OperationReason = "why", OperationToken = "typed",
            });
            Assert.IsFalse(Shown(view, "cancel"));
            Assert.IsTrue(Shown(view, "retry"));
            Assert.IsTrue(Shown(view, "back-to-servers"));
            Assert.IsTrue(Shown(view, "overlay-token-row"));
            Assert.AreEqual("typed", view.Root.Q<TextField>("overlay-token").value);
            Assert.IsTrue(view.Root.Q<TextField>("overlay-token").isPasswordField);
            System.Collections.Generic.List<string> texts = view.Root.Q("progress-steps").Query<Label>().ToList().ConvertAll(label => label.text);
            CollectionAssert.Contains(texts, "FAILED");
            CollectionAssert.Contains(texts, "why");
        }

        [Test]
        public void Dialog_OnALostConnection_ShowsOnlyTheReason()
        {
            MainMenuView view = DialogView(new MainMenuPresentation { OperationFailed = true, OperationLost = true, OperationReason = "why" });
            Assert.AreEqual("CONNECTION LOST", view.Root.Q<Label>("overlay-title").text);
            Assert.IsTrue(Shown(view, "retry"));
            Label reason = view.Root.Q("progress-steps").Q<Label>();
            Assert.AreEqual("why", reason.text);
            Assert.AreEqual(1, view.Root.Q("progress-steps").childCount);
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
        public void StatusProbe_EchoesNonce_AndStaysReadableAsAReply()
        {
            ServerStatus status = new(9, "0.5.0", "Sandbox", 3, 16);
            byte[] request = ServerStatusPacket.Request(0xDEADBEEF);
            Assert.IsTrue(ServerStatusPacket.TryReadRequestNonce(request, out uint nonce));
            Assert.AreEqual(0xDEADBEEF, nonce);
            Assert.IsFalse(ServerStatusPacket.IsRequest(request), "a probe is not the original request");
            Assert.IsFalse(ServerStatusPacket.TryReadRequestNonce(ServerStatusPacket.Request(), out _), "the original request has no nonce");
            Assert.IsFalse(ServerStatusPacket.TryReadRequestNonce(request[..^1], out _), "truncated probe");
            Assert.IsFalse(ServerStatusPacket.TryReadRequestNonce(null, out _));

            byte[] reply = ServerStatusPacket.EncodeReply(status, 1);
            byte[] stamped = ServerStatusPacket.WithNonce(reply, nonce);
            Assert.AreEqual(1u, ReplyNonce(reply), "WithNonce must not touch the template");
            Assert.AreEqual(0xDEADBEEF, ReplyNonce(stamped));
            Assert.IsTrue(ServerStatusPacket.TryDecodeReply(stamped, out ServerStatus decoded, out bool hasNonce, out _));
            Assert.IsTrue(hasNonce);
            Assert.AreEqual("Sandbox", decoded.Name);
            Assert.AreEqual(16, decoded.PlayerCap);

            Assert.IsTrue(ServerStatusPacket.TryDecodeReply(ServerStatusPacket.EncodeReply(status), out _, out bool legacyHasNonce, out _));
            Assert.IsFalse(legacyHasNonce, "the original reply carries no nonce");
            Assert.IsFalse(ServerStatusPacket.TryDecodeReply(stamped[..^3], out _, out _, out _), "truncated probe reply");
            Assert.IsFalse(ServerStatusPacket.TryDecodeReply(reply[..5], out _, out _, out _), "reply cut inside the header");
        }

        private static uint ReplyNonce(byte[] reply)
        {
            Assert.IsTrue(ServerStatusPacket.TryDecodeReply(reply, out _, out bool hasNonce, out uint nonce));
            Assert.IsTrue(hasNonce);
            return nonce;
        }

        [Test]
        public void StatusQuery_ReadsAReplyOverLoopback_AndSendsEveryProbe()
        {
            ServerStatusResult? result = RunQuery(answerProbes: true, answerOriginal: true, out int probes, out int originals);

            Assert.IsTrue(result.HasValue, "no reply within 5 s");
            Assert.IsTrue(result.Value.Reachable);
            Assert.AreEqual("Loopback", result.Value.Status.Name);
            Assert.AreEqual(ServerStatusQuery.ProbeCount, probes, "one probe per sample");
            Assert.AreEqual(0, originals, "a server that answers probes never needs the fallback");
            Assert.GreaterOrEqual(result.Value.PingMilliseconds, 1);
            Assert.Less(result.Value.PingMilliseconds, 1000);
        }

        [Test]
        public void StatusQuery_FallsBackToTheOriginalRequest_ForServersThatPredateProbes()
        {
            ServerStatusResult? result = RunQuery(answerProbes: false, answerOriginal: true, out _, out int originals);

            Assert.IsTrue(result.HasValue, "no reply within 5 s");
            Assert.IsTrue(result.Value.Reachable, "an older server must still list as online");
            Assert.AreEqual("Loopback", result.Value.Status.Name);
            Assert.AreEqual(1, originals);
        }

        [Test]
        public void StatusQuery_ReportsUnreachable_WhenNothingAnswers()
        {
            ServerStatusResult? result = RunQuery(answerProbes: false, answerOriginal: false, out _, out _);

            Assert.IsTrue(result.HasValue, "the query must give up, not hang");
            Assert.IsFalse(result.Value.Reachable);
        }

        /// <summary>Runs one query against a loopback fake server that answers only the request forms asked for.</summary>
        private static ServerStatusResult? RunQuery(bool answerProbes, bool answerOriginal, out int probesSeen, out int originalsSeen)
        {
            EventBasedNetListener listener = new();
            NetManager server = new(listener) { UnconnectedMessagesEnabled = true };
            ServerStatus status = new(9, "0.5.0", "Loopback", 1, 4);
            byte[] originalReply = ServerStatusPacket.EncodeReply(status);
            int probes = 0;
            int originals = 0;
            listener.NetworkReceiveUnconnectedEvent += (remote, reader, type) =>
            {
                byte[] request = reader.GetRemainingBytes();
                reader.Recycle();
                if (ServerStatusPacket.TryReadRequestNonce(request, out uint nonce))
                {
                    probes++;
                    if (answerProbes) server.SendUnconnectedMessage(ServerStatusPacket.EncodeReply(status, nonce), remote);
                }
                else if (ServerStatusPacket.IsRequest(request))
                {
                    originals++;
                    if (answerOriginal) server.SendUnconnectedMessage(originalReply, remote);
                }
            };
            Assert.IsTrue(server.Start(IPAddress.Loopback, IPAddress.IPv6Loopback, 0));
            using ServerStatusQuery query = new();
            ServerStatusResult? result = null;
            query.Completed += (key, value) => result = value;
            try
            {
                query.Query("loopback", "127.0.0.1", (ushort)server.LocalPort);
                System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
                while (result == null && clock.Elapsed.TotalSeconds < ServerStatusQuery.TimeoutSeconds + 2)
                {
                    server.PollEvents();
                    query.Tick();
                    System.Threading.Thread.Sleep(5);
                }
            }
            finally
            {
                server.Stop();
            }

            probesSeen = probes;
            originalsSeen = originals;
            return result;
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
