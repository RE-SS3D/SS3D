using FishNet.Serializing;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Tests;
using SS3D.UI.MachineInterface;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    public class GasPanelSnapshotTests : EditModeTest
    {
        private static readonly GasPanelRow[] Gases = FilterInterfaceViewModel.PreviewGases();

        [Test]
        public void FilterSnapshot_RoundTrips()
        {
            FilterInterfaceSnapshot original = CreateFilter();

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteFilterInterfaceSnapshot(original);
            using PooledReader reader = ReaderPool.Retrieve(writer.GetArraySegment(), null);
            FilterInterfaceSnapshot read = reader.ReadFilterInterfaceSnapshot();

            Assert.AreEqual(original.SelectedGases, read.SelectedGases);
            Assert.AreEqual(original.FlowLitresPerSecond, read.FlowLitresPerSecond);
            Assert.AreEqual(Gases.Length, read.Gases.Length);
            Assert.AreEqual("Plasma", read.Gases[3].Symbol);
            Assert.IsTrue(read.Gases[3].Toxic);
            Assert.AreEqual(Gases[0].Color, read.Gases[0].Color);
            Assert.That(read.Inlet.Fractions[1], Is.EqualTo(0.8f).Within(0.005f));
            Assert.AreEqual(original.FilterOutletPressureKpa, read.FilterOutletPressureKpa);
        }

        [Test]
        public void MixerSnapshot_RoundTrips()
        {
            MixerInterfaceSnapshot original = CreateMixer();
            original.Inlet2Dry = true;

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteMixerInterfaceSnapshot(original);
            using PooledReader reader = ReaderPool.Retrieve(writer.GetArraySegment(), null);
            MixerInterfaceSnapshot read = reader.ReadMixerInterfaceSnapshot();

            Assert.AreEqual(original.Inlet1Percent, read.Inlet1Percent);
            Assert.AreEqual(original.TargetOutletPressureKpa, read.TargetOutletPressureKpa);
            Assert.IsTrue(read.Inlet2Dry);
            Assert.AreEqual(original.Outlet.PressureKpa, read.Outlet.PressureKpa);
        }

        [Test]
        public void MeterSnapshot_RoundTrips()
        {
            MeterInterfaceSnapshot original = CreateMeter(101.3f, 0.2f, 0.8f, 0f, 0f);
            original.History = new[] { 90f, 95f, 101.3f };
            original.Subtitle = "Atmospherics — Pipe Gas Meter";

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteMeterInterfaceSnapshot(original);
            using PooledReader reader = ReaderPool.Retrieve(writer.GetArraySegment(), null);
            MeterInterfaceSnapshot read = reader.ReadMeterInterfaceSnapshot();

            Assert.AreEqual(original.Subtitle, read.Subtitle);
            Assert.AreEqual(original.LineLabel, read.LineLabel);
            Assert.AreEqual(original.TemperatureKelvin, read.TemperatureKelvin);
            CollectionAssert.AreEqual(original.History, read.History);
        }

        [Test]
        public void FilterMapper_ShowsWhatEachOutletGets()
        {
            FilterInterfaceViewModel model = FilterInterfaceSnapshotMapper.ToViewModel(CreateFilter());

            Assert.AreEqual("N2 100.0%", model.Outlet.MixText);
            Assert.AreEqual("O2 100.0%", model.FilterOutlet.MixText);
            Assert.AreEqual("N2 80.0% · O2 20.0%", model.Inlet.MixText);
            Assert.AreEqual("4 types · 1 filtered", model.RegistryText);
            Assert.IsTrue(model.Gases[0].Selected);
            Assert.IsFalse(model.Gases[3].Present);
        }

        [Test]
        public void FilterMapper_MarksAFullOutlet()
        {
            FilterInterfaceSnapshot snapshot = CreateFilter();
            snapshot.Scenario = (byte)FilterScenario.Stalled;
            snapshot.FilterOutletPressureKpa = AtmosPortConstants.FilterOutletLimitKpa;

            FilterInterfaceViewModel model = FilterInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual("STALLED", model.StatusBadgeText);
            Assert.AreEqual("100% · FULL", model.FilterOutlet.CapacityText);
            Assert.IsTrue(model.FilterOutlet.Warn);
            Assert.IsFalse(model.ChassisPowerOk);
        }

        [Test]
        public void MixerMapper_NamesTheDryInlet()
        {
            MixerInterfaceSnapshot snapshot = CreateMixer();
            snapshot.Scenario = (byte)MixerScenario.Stopped;
            snapshot.Inlet2Dry = true;

            MixerInterfaceViewModel model = MixerInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual("Inlet 2 Dry", model.StatusHeadline);
            Assert.AreEqual("Dry — no supply", model.Inlet2.MixText);
            Assert.AreEqual("Inlet 1 · 20%", model.Inlet1.Label);
            Assert.AreEqual("Inlet 2 · 80%", model.Inlet2.Label);
            Assert.IsEmpty(model.Inlet2.Segments);
        }

        [TestCase(101.3f, 0f, MeterScenario.Nominal)]
        [TestCase(4120f, 0f, MeterScenario.Overpressure)]
        [TestCase(186.4f, 0.189f, MeterScenario.Contaminated)]
        [TestCase(186.4f, 0.005f, MeterScenario.Nominal)]
        [TestCase(0.4f, 0f, MeterScenario.Vacuum)]
        public void Meter_ClassifiesItsLine(float pressure, float plasma, MeterScenario expected)
        {
            MeterInterfaceSnapshot snapshot = CreateMeter(pressure, 0.2f, 0.8f - plasma, 0f, plasma);

            Assert.AreEqual(expected, MeterInterfaceSnapshotMapper.ToViewModel(snapshot).Scenario);
        }

        [Test]
        public void MeterMapper_SortsGasesAndWarnsOfTheToxicOne()
        {
            MeterInterfaceViewModel model = MeterInterfaceSnapshotMapper.ToViewModel(CreateMeter(100f, 0.14f, 0.6f, 0.06f, 0.2f));

            Assert.AreEqual("N2", model.Gases[0].Symbol);
            Assert.AreEqual("Plasma", model.Gases[1].Symbol);
            Assert.IsTrue(model.Gases[1].Hazard);
            Assert.AreEqual("20.0 kPa", model.Gases[1].PressureText);
            Assert.AreEqual("Plasma above 1% — check upstream filters.", model.StatusSubline);
        }

        [Test]
        public void Describe_NamesTheThreeRichestGases_AndKeepsEverySegment()
        {
            var segments = new List<GasMixSegment>();

            string text = GasMixDescriber.Describe(Gases, new[] { 0.1f, 0.6f, 0.2f, 0.1f }, segments);

            Assert.AreEqual("N2 60.0% · CO2 20.0% · O2 10.0%", text);
            Assert.AreEqual(4, segments.Count);
        }

        [Test]
        public void Rows_ComeFromTheGasDefinitions()
        {
            GasDefinition plasma = ScriptableObject.CreateInstance<GasDefinition>();
            try
            {
                GasPanelRow[] rows = GasPanelReadings.BuildRows(new[] { plasma });
                Assert.AreEqual(1, rows.Length);
                Assert.AreEqual(plasma.Symbol, rows[0].Symbol);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(plasma);
            }
        }

        private static FilterInterfaceSnapshot CreateFilter() => new()
        {
            InterfaceId = MachineInterfaceIds.Filter,
            ModelLabel = "FLT-04 · gas filter unit",
            DeviceTitle = "FLT-04",
            Subtitle = "Atmospherics Distro — Gas Filter",
            PowerOk = true,
            Powered = true,
            Connected = true,
            Scenario = (byte)FilterScenario.Filtering,
            FlowLitresPerSecond = 50,
            SelectedGases = 1UL,
            Gases = Gases,
            Inlet = new GasLineReading { PressureKpa = 300f, Fractions = new[] { 0.2f, 0.8f, 0f, 0f } },
            OutletPressureKpa = 200f,
            FilterOutletPressureKpa = 1800f,
        };

        private static MixerInterfaceSnapshot CreateMixer() => new()
        {
            InterfaceId = MachineInterfaceIds.Mixer,
            ModelLabel = "MIX-11 · gas mixer unit",
            DeviceTitle = "MIX-11",
            Subtitle = "Atmospherics Distro — Gas Mixer",
            PowerOk = true,
            Powered = true,
            Connected = true,
            Scenario = (byte)MixerScenario.Mixing,
            Inlet1Percent = 20,
            TargetOutletPressureKpa = 4500,
            Gases = Gases,
            Inlet1 = new GasLineReading { PressureKpa = 2400f, Fractions = new[] { 1f, 0f, 0f, 0f } },
            Inlet2 = new GasLineReading { PressureKpa = 0f, Fractions = new[] { 0f, 0f, 0f, 0f } },
            Outlet = new GasLineReading { PressureKpa = 1600f, Fractions = new[] { 0.2f, 0.8f, 0f, 0f } },
        };

        private static MeterInterfaceSnapshot CreateMeter(float pressure, params float[] fractions) => new()
        {
            InterfaceId = MachineInterfaceIds.Meter,
            ModelLabel = "GM-02 · pipe gas meter",
            DeviceTitle = "GM-02",
            LineLabel = "Supply line · 14 segments",
            Connected = true,
            TemperatureKelvin = 293f,
            Moles = 10f,
            VolumeLitres = 7000f,
            Gases = Gases,
            Line = new GasLineReading { PressureKpa = pressure, Fractions = fractions },
        };
    }
}
