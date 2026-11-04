using System;
using System.IO;
using NUnit.Framework;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace EditorTests
{
    public class PlayerSettingsStoreTests
    {
        private string _directory;
        private string SettingsPath => Path.Combine(_directory, "settings.json");

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "ss3d-settings-" + Guid.NewGuid());

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void SavedAudioSettingsSurviveARestart()
        {
            PlayerSettings settings = new();
            settings.Audio.Master = 0.35f;
            settings.Audio.Music = 0.1f;
            settings.Audio.Sfx = 0.95f;
            settings.Audio.Ambience = 0.4f;
            settings.Audio.Personal = 0.6f;
            settings.Audio.MuteAll = true;
            settings.Audio.MuteWhenUnfocused = false;
            Assert.That(new PlayerSettingsStore(SettingsPath).Save(settings), Is.True);

            PlayerAudioSettings loaded = new PlayerSettingsStore(SettingsPath).Load().Audio;
            Assert.That(loaded.Master, Is.EqualTo(0.35f));
            Assert.That(loaded.Music, Is.EqualTo(0.1f));
            Assert.That(loaded.Sfx, Is.EqualTo(0.95f));
            Assert.That(loaded.Ambience, Is.EqualTo(0.4f));
            Assert.That(loaded.Personal, Is.EqualTo(0.6f));
            Assert.That(loaded.MuteAll, Is.True);
            Assert.That(loaded.MuteWhenUnfocused, Is.False);
        }

        [Test]
        public void SavedFileRecordsItsFormatVersion()
        {
            new PlayerSettingsStore(SettingsPath).Save(new PlayerSettings());

            Assert.That(File.ReadAllText(SettingsPath), Does.Contain("\"Version\": 1"));
        }

        [TestCase("invalid json")]
        [TestCase("")]
        [TestCase("[1, 2, 3]")]
        public void CorruptFileIsKeptAsBackupAndDefaultsLoad(string corrupt)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SettingsPath, corrupt);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("settings"));

            PlayerSettings loaded = new PlayerSettingsStore(SettingsPath).Load();

            Assert.That(loaded.Audio.Master, Is.EqualTo(0.8f));
            Assert.That(loaded.Audio.MuteWhenUnfocused, Is.True);
            Assert.That(File.ReadAllText(SettingsPath + ".bak"), Is.EqualTo(corrupt));
        }

        [Test]
        public void KeysMissingFromTheFileKeepTheirDefaults()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SettingsPath, "{\"Version\":1,\"Audio\":{\"Music\":0.25}}");

            PlayerAudioSettings loaded = new PlayerSettingsStore(SettingsPath).Load().Audio;

            Assert.That(loaded.Music, Is.EqualTo(0.25f));
            Assert.That(loaded.Master, Is.EqualTo(0.8f));
            Assert.That(loaded.MuteWhenUnfocused, Is.True);
        }

        [Test]
        public void UnknownKeysAreIgnored()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SettingsPath,
                "{\"Version\":1,\"Brightness\":0.6,\"Audio\":{\"Voice\":0.1,\"Sfx\":0.3}}");

            Assert.That(new PlayerSettingsStore(SettingsPath).Load().Audio.Sfx, Is.EqualTo(0.3f));
        }

        [Test]
        public void MissingFileLoadsFirstLaunchDefaultsWithoutCreatingIt()
        {
            PlayerAudioSettings loaded = new PlayerSettingsStore(SettingsPath).Load().Audio;

            Assert.That(loaded.Master, Is.EqualTo(0.8f));
            Assert.That(loaded.Personal, Is.EqualTo(0.8f));
            Assert.That(loaded.MuteAll, Is.False);
            Assert.That(loaded.MuteWhenUnfocused, Is.True);
            Assert.That(File.Exists(SettingsPath), Is.False);
        }

        [Test]
        public void OutOfRangeVolumesAreClampedOnLoad()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SettingsPath, "{\"Version\":1,\"Audio\":{\"Master\":3.5,\"Music\":-1}}");

            PlayerAudioSettings loaded = new PlayerSettingsStore(SettingsPath).Load().Audio;

            Assert.That(loaded.Master, Is.EqualTo(1f));
            Assert.That(loaded.Music, Is.EqualTo(0f));
        }

        [Test]
        public void SavedDisplaySettingsSurviveARestart()
        {
            PlayerSettings settings = new();
            settings.Display.ResolutionWidth = 1600;
            settings.Display.ResolutionHeight = 900;
            settings.Display.WindowMode = PlayerWindowMode.Windowed;
            settings.Display.VSync = false;
            settings.Display.FrameCap = 144;
            settings.Display.Quality = PlayerQualityTier.Low;
            settings.Display.RenderScale = 0.67f;
            settings.Display.Upscaler = PlayerUpscaler.Fsr1;
            settings.Display.AntiAliasing = PlayerAntiAliasing.Taa;
            settings.Display.UiScale = 1.25f;
            settings.Display.ShowFps = true;
            settings.Display.ShowLogIndicator = true;
            new PlayerSettingsStore(SettingsPath).Save(settings);

            PlayerDisplaySettings loaded = new PlayerSettingsStore(SettingsPath).Load().Display;
            Assert.That((loaded.ResolutionWidth, loaded.ResolutionHeight), Is.EqualTo((1600, 900)));
            Assert.That(loaded.WindowMode, Is.EqualTo(PlayerWindowMode.Windowed));
            Assert.That(loaded.VSync, Is.False);
            Assert.That(loaded.FrameCap, Is.EqualTo(144));
            Assert.That(loaded.Quality, Is.EqualTo(PlayerQualityTier.Low));
            Assert.That(loaded.RenderScale, Is.EqualTo(0.67f));
            Assert.That(loaded.Upscaler, Is.EqualTo(PlayerUpscaler.Fsr1));
            Assert.That(loaded.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Taa));
            Assert.That(loaded.UiScale, Is.EqualTo(1.25f));
            Assert.That(loaded.ShowFps, Is.True);
            Assert.That(loaded.ShowLogIndicator, Is.True);
        }

        [Test]
        public void FirstLaunchDisplayDefaultsFollowDecision14()
        {
            PlayerDisplaySettings display = new PlayerSettingsStore(SettingsPath).Load().Display;

            Assert.That((display.ResolutionWidth, display.ResolutionHeight), Is.EqualTo((0, 0)), "0x0 means native");
            Assert.That(display.WindowMode, Is.EqualTo(PlayerWindowMode.Borderless));
            Assert.That(display.VSync, Is.True);
            Assert.That(display.FrameCap, Is.EqualTo(0));
            Assert.That(display.Quality, Is.EqualTo(PlayerQualityTier.High));
            Assert.That(display.RenderScale, Is.EqualTo(1f));
            Assert.That(display.Upscaler, Is.EqualTo(PlayerUpscaler.Stp));
            Assert.That(display.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Smaa));
            Assert.That(display.UiScale, Is.EqualTo(1f));
            Assert.That(display.ShowFps, Is.False);
            Assert.That(display.ShowLogIndicator, Is.False);
        }

        [Test]
        public void HandEditedDisplayValuesSnapToTheOfferedChoices()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SettingsPath, "{\"Version\":1,\"Display\":{\"FrameCap\":100,\"RenderScale\":0.7," +
                "\"UiScale\":1.93,\"Quality\":9,\"WindowMode\":-1,\"Upscaler\":7,\"AntiAliasing\":12}}");

            PlayerDisplaySettings display = new PlayerSettingsStore(SettingsPath).Load().Display;

            Assert.That(display.FrameCap, Is.EqualTo(120), "nearest of Off/30/60/120/144/240");
            Assert.That(display.RenderScale, Is.EqualTo(0.67f), "nearest of 100/77/67/50 %");
            Assert.That(display.UiScale, Is.EqualTo(1.5f), "75-150 %");
            Assert.That(display.Quality, Is.EqualTo(PlayerQualityTier.High));
            Assert.That(display.WindowMode, Is.EqualTo(PlayerWindowMode.Borderless));
            Assert.That(display.Upscaler, Is.EqualTo(PlayerUpscaler.Stp));
            Assert.That(display.AntiAliasing, Is.EqualTo(PlayerAntiAliasing.Smaa));
        }

        [TestCase(1.234f, 1.23f)]
        [TestCase(0.5f, 0.75f)]
        [TestCase(2f, 1.5f)]
        public void UiScaleMovesInOnePercentStepsWithinRange(float value, float snapped)
        {
            Assert.That(PlayerDisplaySettings.SnapUiScale(value), Is.EqualTo(snapped).Within(0.0001f));
        }

        [Test]
        public void SavedControlsSurviveARestart()
        {
            PlayerSettings settings = new();
            settings.Controls.BindingOverrides = "{\"bindings\":[{\"action\":\"Hotkeys/Use\",\"path\":\"<Keyboard>/g\"}]}";
            settings.Controls.SystemBindingOverrides = "{\"bindings\":[]}";
            new PlayerSettingsStore(SettingsPath).Save(settings);

            PlayerControlsSettings loaded = new PlayerSettingsStore(SettingsPath).Load().Controls;

            Assert.That(loaded.BindingOverrides, Is.EqualTo(settings.Controls.BindingOverrides));
            Assert.That(loaded.SystemBindingOverrides, Is.EqualTo("{\"bindings\":[]}"));
        }
    }
}
