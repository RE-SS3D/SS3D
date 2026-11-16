using NUnit.Framework;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Audio;
using UnityEngine;
using CellRead = SS3D.Systems.Atmospherics.Visualization.AtmosCellVisualEncoding.CellRead;

namespace EditorTests
{
    public class AudioAirStateTests
    {
        private const float FarAway = 10f;

        [Test]
        public void Air_IsFullInARoomAndGoneInVacuum()
        {
            Assert.AreEqual(1f, AudioAirState.AirFromPressure(101.325f));
            Assert.AreEqual(0f, AudioAirState.AirFromPressure(0f));
        }

        [Test]
        public void Hearing_FallsSteadilyWithPressure()
        {
            float last = 1f;
            for (float kpa = AudioAirState.FullPressureKpa; kpa >= 0f; kpa -= 5f)
            {
                float hearing = AudioAirState.Hearing(AudioAirState.AirFromPressure(kpa), FarAway);
                Assert.LessOrEqual(hearing, last);
                last = hearing;
            }

            Assert.AreEqual(0f, last);
        }

        [Test]
        public void Vacuum_SilencesTheRoom_ButNotWhatYouTouch()
        {
            Assert.AreEqual(0f, AudioAirState.Hearing(0f, FarAway));
            Assert.AreEqual(AudioAirState.TouchHearing, AudioAirState.Hearing(0f, 0.5f));
            Assert.AreEqual(AudioAirState.TouchHearing, AudioAirState.Hearing(0f, 1f));
        }

        [Test]
        public void Hearing_FadesOutAcrossTheTouchRange()
        {
            float edge = AudioAirState.Hearing(0f, 1.4f);
            Assert.Greater(edge, 0f);
            Assert.Less(edge, AudioAirState.TouchHearing);
            Assert.AreEqual(0f, AudioAirState.Hearing(0f, 1.75f));
        }

        [Test]
        public void Hearing_InFullAir_IsUntouchedAtAnyDistance()
        {
            Assert.AreEqual(1f, AudioAirState.Hearing(1f, 0f));
            Assert.AreEqual(1f, AudioAirState.Hearing(1f, FarAway));
        }

        [Test]
        public void Cutoff_ClearsAtFullHearing_AndDropsWithIt()
        {
            Assert.AreEqual(AudioOcclusionState.ClearCutoffHz, AudioAirState.CutoffHz(1f), 0.5f);
            Assert.Less(AudioAirState.CutoffHz(0.5f), AudioOcclusionState.ClearCutoffHz);
            Assert.Less(AudioAirState.CutoffHz(AudioAirState.TouchHearing), 1000f);
        }

        [Test]
        public void ResolveAir_MissingDataIsAir_SoALateJoinerHearsTheRoom()
        {
            Assert.AreEqual(1f, AudioAirState.ResolveAir(CellRead.Unknown, 0f, 0.4f, inOpenSpace: false));
        }

        [Test]
        public void ResolveAir_MissingDataInOpenSpaceIsVacuum()
        {
            Assert.AreEqual(0f, AudioAirState.ResolveAir(CellRead.Unknown, 0f, 1f, inOpenSpace: true));
        }

        [Test]
        public void ResolveAir_AWallTileKeepsTheLastReading()
        {
            Assert.AreEqual(0.4f, AudioAirState.ResolveAir(CellRead.Blocked, 0f, 0.4f, inOpenSpace: false));
        }

        [Test]
        public void ResolveAir_ARealReadingBeatsTheFallback()
        {
            Assert.AreEqual(0f, AudioAirState.ResolveAir(CellRead.Open, 0f, 1f, inOpenSpace: false));
            Assert.AreEqual(1f, AudioAirState.ResolveAir(CellRead.Open, 101f, 0f, inOpenSpace: true));
        }

        [Test]
        public void Wind_IsSilentInStillAir()
        {
            for (int layer = 0; layer < AudioAirState.WindLayerCount; layer++)
            {
                Assert.AreEqual(0f, AudioAirState.WindGain(layer, 0f));
            }
        }

        [Test]
        public void Wind_MovesUpTheLayersAsItRises()
        {
            Assert.Greater(AudioAirState.WindGain(0, 1f), 0f);
            Assert.AreEqual(0f, AudioAirState.WindGain(1, 1f));

            Assert.Greater(AudioAirState.WindGain(1, 3f), AudioAirState.WindGain(0, 3f));
            Assert.AreEqual(0f, AudioAirState.WindGain(2, 3f));

            Assert.AreEqual(1f, AudioAirState.WindGain(2, 5f));
            Assert.AreEqual(0f, AudioAirState.WindGain(0, 5f));
            Assert.AreEqual(0f, AudioAirState.WindGain(1, 5f));
        }

        [Test]
        public void Wind_NeverDropsOutBetweenLayers()
        {
            for (float speed = 1f; speed <= 5f; speed += 0.1f)
            {
                float total = 0f;
                for (int layer = 0; layer < AudioAirState.WindLayerCount; layer++)
                {
                    total += AudioAirState.WindGain(layer, speed);
                }

                Assert.Greater(total, 0.3f, $"at {speed} m/s");
            }
        }

        [Test]
        public void ReadCell_ReadsEachKindOfTile()
        {
            float[] pressure = { 101f, 50f, 30f, 20f };
            byte[] mask =
            {
                AtmosCellVisualEncoding.MaskSimulated, AtmosCellVisualEncoding.MaskVacuum,
                AtmosCellVisualEncoding.MaskBlocked, AtmosCellVisualEncoding.MaskEmpty,
            };
            var bounds = new Vector4(10f, 20f, 2f, 2f);

            Assert.AreEqual(CellRead.Open, Read(10.2f, 19.8f, out float room));
            Assert.AreEqual(101f, room);
            Assert.AreEqual(CellRead.Open, Read(11f, 20f, out float vacuum));
            Assert.AreEqual(0f, vacuum);
            Assert.AreEqual(CellRead.Blocked, Read(10f, 21f, out _));
            Assert.AreEqual(CellRead.Unknown, Read(11f, 21f, out _));
            Assert.AreEqual(CellRead.Unknown, Read(40f, 21f, out _));
            Assert.AreEqual(CellRead.Unknown, Read(9f, 20f, out _));

            CellRead Read(float x, float z, out float kpa) =>
                AtmosCellVisualEncoding.ReadCell(pressure, mask, bounds, x, z, out kpa);
        }

        [Test]
        public void ReadCell_WithNoAtlasIsUnknown()
        {
            Assert.AreEqual(CellRead.Unknown,
                AtmosCellVisualEncoding.ReadCell(null, null, Vector4.zero, 0f, 0f, out _));
        }

        [Test]
        public void DecodeWindSpeed_UndoesTheFlowEncoding()
        {
            Vector2 flow = AtmosCellVisualEncoding.EncodeFlow(new Vector3(2f, 0f, 0f));
            Assert.AreEqual(2f, AtmosCellVisualEncoding.DecodeWindSpeed(flow), 0.001f);
            Assert.AreEqual(0f, AtmosCellVisualEncoding.DecodeWindSpeed(Vector2.zero));
        }
    }
}
