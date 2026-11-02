using System.Linq;
using SS3D.Core;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using UnityEditor;

namespace SS3D.Editor.Automation
{
    public static partial class LobbyLifecyclePilot
    {
        private const string CriticalSpeechMode = "critical-speech";
        private const string SpeechProbePrefix = "critical-speech-probe";
        private static int _speechProbeCount;
        private static Human _speechBody;

        [MenuItem("SS3D/Testing/Run Critical Speech Regressions")]
        public static void RunCriticalSpeechRegressions() => Start(CriticalSpeechMode);

        private static void SendSpeechProbes()
        {
            CommsSubSystem comms = SubSystems.Get<CommsSubSystem>();
            LocalSpeechEmitter emitter = _speechBody.GetComponent<LocalSpeechEmitter>();
            comms.HandleSpeakRequest(emitter, SpeechProbePrefix + " speak", SpeechMode.Speak);
            comms.HandleSpeakRequest(emitter, SpeechProbePrefix + " whisper", SpeechMode.Whisper);
            comms.HandleSpeakRequest(emitter, SpeechProbePrefix + " shout", SpeechMode.Shout);
            comms.HandleRadioRequest(emitter, comms.ChannelSettings.GetWritableRadioChannels()[0].Id,
                SpeechProbePrefix + " radio");
            comms.HandleAnnouncementRequest(emitter, SpeechProbePrefix + " announcement");
        }

        private static Step[] BuildCriticalSpeechSteps() => BuildSandboxSteps().Take(4).Concat(new Step[]
        {
            new Step
            {
                Name = "Healthy body can speak, whisper, shout, radio and announce",
                Enter = () =>
                {
                    _speechBody = (Human)SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                    _speechProbeCount = 0;
                    CommsSubSystem comms = SubSystems.Get<CommsSubSystem>();
                    comms.OnLocalSpeechReceived += (_, speech) =>
                    {
                        if (speech.Text.StartsWith(SpeechProbePrefix)) _speechProbeCount++;
                    };
                    comms.OnCommsMessageReceived += message =>
                    {
                        if (message.Text.StartsWith(SpeechProbePrefix)) _speechProbeCount++;
                    };
                    Require(_speechBody.GetComponent<LocalSpeechEmitter>().CanSpeak, "Healthy body has speech capability");
                    SendSpeechProbes();
                },
                Until = () => _speechProbeCount == 5,
            },
            new Step
            {
                Name = "Critical state refuses all in-character speech on the server",
                Enter = () =>
                {
                    _speechBody.GetComponent<HumanHealthController>().TickHealth(
                        toxinIntake: HealthConstants.CriticalToxinConcentration + 5f);
                    Require(!_speechBody.GetComponent<LocalSpeechEmitter>().CanSpeak, "Critical body loses speech capability");
                    SendSpeechProbes();
                    Settle(1);
                },
                Until = Settled,
                Verify = () => Require(_speechProbeCount == 5, "No critical speak/whisper/shout/radio/announcement is delivered"),
            },
            new Step
            {
                Name = "Treatment restores speech",
                Enter = () =>
                {
                    _speechBody.GetComponent<HumanHealthController>().RestoreSystemicPools();
                    Require(_speechBody.GetComponent<LocalSpeechEmitter>().CanSpeak, "Recovered body regains speech capability");
                    SendSpeechProbes();
                },
                Until = () => _speechProbeCount == 10,
            },
            new Step
            {
                Name = "Death refuses speech even after a direct kill",
                Enter = () =>
                {
                    _speechBody.Kill();
                    Require(!_speechBody.GetComponent<LocalSpeechEmitter>().CanSpeak, "Dead body cannot speak");
                    SendSpeechProbes();
                    Settle(1);
                },
                Until = Settled,
                Verify = () => Require(_speechProbeCount == 10, "No corpse speech is delivered"),
            },
        }).ToArray();
    }
}
