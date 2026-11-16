using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.BugReports;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>The bug report dialog's pure parts (<c>sandbox-server.md</c> §7): limits, text, bundle and issue link.</summary>
    public sealed class BugReportTests
    {
        private string _folder;

        [SetUp]
        public void CreateFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), "ss3d-bugreport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        [Test]
        public void RateLimiter_AllowsOnePerMinutePerPlayer()
        {
            BugReportRateLimiter limiter = new();

            Assert.IsTrue(limiter.TryConsume("alice", 10f, out _));
            Assert.IsFalse(limiter.TryConsume("alice", 52f, out float retry));
            Assert.AreEqual(18f, retry, 0.001f);
            Assert.IsTrue(limiter.TryConsume("bob", 52f, out _), "Another player is not limited");
            Assert.IsTrue(limiter.TryConsume("alice", 70f, out _));
        }

        [Test]
        public void RateLimiter_RefusalDoesNotRestartTheClock()
        {
            BugReportRateLimiter limiter = new();
            limiter.TryConsume("alice", 0f, out _);
            limiter.TryConsume("alice", 59f, out _);

            Assert.AreEqual(0f, limiter.SecondsUntilAllowed("alice", 60f));
        }

        [Test]
        public void Text_FormatsClockAndCountdown()
        {
            Assert.AreEqual("00:14:32", BugReportText.FormatClock(14 * 60 + 32));
            Assert.AreEqual("0:42", BugReportText.FormatCountdown(41.2f));
            Assert.AreEqual("1:00", BugReportText.FormatCountdown(60f));
        }

        [Test]
        public void Text_CaptureLineNamesRoundOrLobby()
        {
            BugReportClientInfo info = new() { roundId = 1187, roundSeconds = 872, roundState = "Ongoing" };
            Assert.AreEqual("ROUND #1187 · CAPTURED AT 00:14:32", BugReportText.CaptureLine(info));

            info.roundState = "Lobby";
            info.capturedAtUtc = DateTime.UtcNow.ToString("o");
            StringAssert.StartsWith("LOBBY · CAPTURED AT ", BugReportText.CaptureLine(info));
        }

        [Test]
        public void Text_SeverityRoundTripsAndBlankMeansUntriaged()
        {
            foreach (BugReportSeverity severity in Enum.GetValues(typeof(BugReportSeverity)))
                Assert.AreEqual(severity, BugReportText.ParseSeverity(BugReportText.SeverityKey(severity)));
            Assert.AreEqual(BugReportSeverity.None, BugReportText.ParseSeverity("critical"));
        }

        [Test]
        public void Text_ClampsLogTailToNewestLines()
        {
            string[] lines = Enumerable.Range(0, 100).Select(i => i + new string('x', 1000)).ToArray();

            string[] clamped = BugReportText.ClampLog(lines);

            Assert.AreEqual(BugReportText.MaxLogLines, clamped.Length);
            StringAssert.StartsWith("40", clamped[0]);
            Assert.IsTrue(clamped.All(line => line.Length <= BugReportText.MaxLogLineLength));
        }

        [Test]
        public void Text_MetadataListsElevenFieldsAndSwapsLogRowLocally()
        {
            BugReportClientInfo info = new() { context = "IN ROUND · ALIVE", hasPosition = true, area = "Bar" };

            var remote = BugReportText.MetadataRows(info, 42, local: false);
            var local = BugReportText.MetadataRows(info, 42, local: true);

            Assert.AreEqual(11, remote.Count);
            Assert.AreEqual("Recent log", remote[10].Key);
            Assert.AreEqual("42 lines", remote[10].Value);
            Assert.AreEqual("Log files", local[10].Key);
            Assert.AreEqual("In round · alive", remote[3].Value);
            StringAssert.StartsWith("Bar (", remote[4].Value);
        }

        [Test]
        public void Bundle_NamesByRoundAndTime()
        {
            BugReportClientInfo info = new() { roundId = 1187, roundSeconds = 872, roundState = "Ongoing" };
            Assert.AreEqual("bug-1187-001432", BugReportBundle.BaseName(info, DateTime.Now));

            info.roundState = "Lobby";
            Assert.AreEqual("bug-lobby-213359", BugReportBundle.BaseName(info, new DateTime(2026, 11, 1, 21, 33, 59)));
        }

        [Test]
        public void Bundle_WritesReportScreenshotLogsAndMap()
        {
            string log = Path.Combine(_folder, "LogHost.json");
            File.WriteAllText(log, "{\"line\":1}");
            BugReportBundleReport report = new() { summary = "Wrench clips", client = new BugReportClientInfo { map = "ministation" } };

            bool written = BugReportBundle.TryWrite(Path.Combine(_folder, "Exports"), "bug-1-000001", report,
                new byte[] { 1, 2, 3 }, new[] { log, log, Path.Combine(_folder, "missing.log") }, "ministation", "{\"map\":true}",
                out string zipPath, out string error);

            Assert.IsTrue(written, error);
            using ZipArchive archive = new(File.OpenRead(zipPath), ZipArchiveMode.Read);
            string[] names = archive.Entries.Select(entry => entry.FullName).OrderBy(name => name).ToArray();
            CollectionAssert.AreEqual(new[] { "logs/LogHost.json", "map/ministation.json", "report.json", "screenshot.png" }, names);
            using StreamReader reader = new(archive.GetEntry("report.json").Open());
            StringAssert.Contains("Wrench clips", reader.ReadToEnd());
        }

        [Test]
        public void Bundle_NeverOverwritesAnEarlierReport()
        {
            BugReportBundleReport report = new() { summary = "a", client = new BugReportClientInfo() };
            string exports = Path.Combine(_folder, "Exports");

            BugReportBundle.TryWrite(exports, "bug-1-000001", report, null, null, null, null, out string first, out _);
            BugReportBundle.TryWrite(exports, "bug-1-000001", report, null, null, null, null, out string second, out _);

            Assert.AreNotEqual(first, second);
            Assert.IsTrue(File.Exists(first) && File.Exists(second));
            StringAssert.EndsWith("bug-1-000001-2.zip", second);
        }

        [Test]
        public void IssueLink_PrefillsTheFormFields()
        {
            BugReportBundleReport report = new()
            {
                summary = "Area tool & undo",
                details = "Steps",
                severity = "major",
                client = new BugReportClientInfo { version = "0.1.1-dev.12+5da58b9", context = "MAP EDITOR · AREA TOOL" },
            };

            string url = BugReportIssueLink.Build(report, "bug-1-000001.zip", mapEditor: true);

            StringAssert.StartsWith(BugReportIssueLink.NewIssueUrl + "?template=MapBug.yml", url);
            StringAssert.Contains("title=Area%20tool%20%26%20undo", url);
            StringAssert.Contains("version=0.1.1-dev.12%2B5da58b9", url);
            StringAssert.Contains("bug-1-000001.zip", url);
            StringAssert.Contains("Bug.yml", BugReportIssueLink.Build(report, "x.zip", mapEditor: false));
        }

        [Test]
        public void LogTail_KeepsWarningsAndErrorsOnly()
        {
            int before = BugReportLogTail.Snapshot().Length;
            BugReportLogTail.Record("plain info", "", LogType.Log);
            BugReportLogTail.Record("careful", "", LogType.Warning);
            BugReportLogTail.Record("broke", "At.Frame()\nAt.Caller()", LogType.Error);

            string[] lines = BugReportLogTail.Snapshot();

            Assert.IsFalse(lines.Any(line => line.Contains("plain info")));
            StringAssert.Contains("[Warning] careful", lines[^2]);
            StringAssert.Contains("[Error] broke @ At.Frame()", lines[^1]);
            Assert.LessOrEqual(lines.Length, BugReportText.MaxLogLines);
            Assert.GreaterOrEqual(lines.Length, Math.Min(before + 2, BugReportText.MaxLogLines));
        }
    }
}
