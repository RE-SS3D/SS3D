using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: the server refuses speak, whisper, shout, radio and announcements from a critical or dead body and allows them again after treatment (comms §speech gates, LocalSpeechEmitter.CanSpeak; the old critical-speech pilot) · Not cheaper because: needs the server speech handlers, the body's health state and the host's receipt of each message together
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Speech)]
    public class SpeechScenarios : ScenarioFixture
    {
        private const string Probe = "scenario-speech-probe";
        private const int ProbesPerRound = 5;
        private int _heard;

        [UnityTest]
        public IEnumerator CriticalAndDead_RefuseSpeech_TreatmentRestoresIt()
        {
            CommsSubSystem comms = SubSystems.Get<CommsSubSystem>();
            comms.OnLocalSpeechReceived += OnLocalSpeech;
            comms.OnCommsMessageReceived += OnCommsMessage;
            try
            {
                LocalSpeechEmitter emitter = Body.GetComponent<LocalSpeechEmitter>();
                Assert.IsTrue(emitter.CanSpeak, "A healthy body can speak");
                SendProbes(comms, emitter);
                yield return WaitFor(() => _heard == ProbesPerRound, "speak, whisper, shout, radio and announcement all arrive");

                MakeCritical(Body);
                Assert.IsFalse(emitter.CanSpeak, "A critical body cannot speak");
                SendProbes(comms, emitter);
                yield return Hold(1f);
                Assert.AreEqual(ProbesPerRound, _heard, "Nothing a critical body says is delivered");

                yield return HealFully();
                Assert.IsTrue(emitter.CanSpeak, "Treatment restores speech");
                SendProbes(comms, emitter);
                yield return WaitFor(() => _heard == 2 * ProbesPerRound, "speech arrives again after treatment");

                yield return Console.Expect($"kill {ServerHelpers.LocalCkey}", "Player killed");
                Assert.IsFalse(emitter.CanSpeak, "A dead body cannot speak");
                SendProbes(comms, emitter);
                yield return Hold(1f);
                Assert.AreEqual(2 * ProbesPerRound, _heard, "Nothing a corpse says is delivered");
            }
            finally
            {
                comms.OnLocalSpeechReceived -= OnLocalSpeech;
                comms.OnCommsMessageReceived -= OnCommsMessage;
            }
        }

        private static void SendProbes(CommsSubSystem comms, LocalSpeechEmitter emitter)
        {
            comms.HandleSpeakRequest(emitter, Probe + " speak", SpeechMode.Speak);
            comms.HandleSpeakRequest(emitter, Probe + " whisper", SpeechMode.Whisper);
            comms.HandleSpeakRequest(emitter, Probe + " shout", SpeechMode.Shout);
            comms.HandleRadioRequest(emitter, comms.ChannelSettings.GetWritableRadioChannels()[0].Id, Probe + " radio");
            comms.HandleAnnouncementRequest(emitter, Probe + " announcement");
        }

        private void OnLocalSpeech(Entity speaker, SpeechEvent speech)
        {
            if (speech.Text.StartsWith(Probe))
                _heard++;
        }

        private void OnCommsMessage(CommsMessage message)
        {
            if (message.Text.StartsWith(Probe))
                _heard++;
        }
    }
}
