using FishNet.Serializing;
using NUnit.Framework;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.IdAccess;
using SS3D.Tests;
using SS3D.UI.MachineInterface;
using System;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class MachineInterfaceSnapshotTests : EditModeTest
    {
        [Test]
        public void ApcInterfaceSnapshotSerializer_RoundTrips()
        {
            ApcInterfaceSnapshot original = CreateApcSnapshot();

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteApcInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            ApcInterfaceSnapshot roundTripped = reader.ReadApcInterfaceSnapshot();

            Assert.AreEqual(original.MachineObjectId, roundTripped.MachineObjectId);
            Assert.AreEqual(original.InterfaceId, roundTripped.InterfaceId);
            Assert.AreEqual(original.Title, roundTripped.Title);
            Assert.AreEqual(original.Channels, roundTripped.Channels);
            Assert.AreEqual(original.PowerState, roundTripped.PowerState);
            Assert.AreEqual(original.GridInputKw, roundTripped.GridInputKw);
            Assert.AreEqual(original.LoadOutputKw, roundTripped.LoadOutputKw);
            Assert.AreEqual(original.BatteryCharge, roundTripped.BatteryCharge);
            Assert.AreEqual(original.BatteryState, roundTripped.BatteryState);
            Assert.AreEqual(original.LightingLoadKw, roundTripped.LightingLoadKw);
            Assert.AreEqual(original.EquipmentLoadKw, roundTripped.EquipmentLoadKw);
            Assert.AreEqual(original.EnvironmentLoadKw, roundTripped.EnvironmentLoadKw);
            Assert.AreEqual(original.MultipleApcsInArea, roundTripped.MultipleApcsInArea);
            Assert.AreEqual(original.AccessGranted, roundTripped.AccessGranted);
            Assert.AreEqual(original.AccessScanning, roundTripped.AccessScanning);
            Assert.AreEqual(original.AccessDenied, roundTripped.AccessDenied);
            Assert.AreEqual(original.DiagnosticCount, roundTripped.DiagnosticCount);
            Assert.AreEqual(original.Diagnostic0.Glyph, roundTripped.Diagnostic0.Glyph);
            Assert.AreEqual(original.Diagnostic0.Text, roundTripped.Diagnostic0.Text);
            Assert.AreEqual(original.Diagnostic0.Tone, roundTripped.Diagnostic0.Tone);
        }

        [Test]
        public void SmesInterfaceSnapshotSerializer_RoundTrips()
        {
            SmesInterfaceSnapshot original = CreateSmesSnapshot();

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteSmesInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            SmesInterfaceSnapshot roundTripped = reader.ReadSmesInterfaceSnapshot();

            Assert.AreEqual(original.MachineObjectId, roundTripped.MachineObjectId);
            Assert.AreEqual(original.InterfaceId, roundTripped.InterfaceId);
            Assert.AreEqual(original.Title, roundTripped.Title);
            Assert.AreEqual(original.PowerState, roundTripped.PowerState);
            Assert.AreEqual(original.ChargePct, roundTripped.ChargePct);
            Assert.AreEqual(original.ChargeTrend, roundTripped.ChargeTrend);
            Assert.AreEqual(original.InputCurrentKw, roundTripped.InputCurrentKw);
            Assert.AreEqual(original.OutputCurrentKw, roundTripped.OutputCurrentKw);
            Assert.AreEqual(original.InputMaxKw, roundTripped.InputMaxKw);
            Assert.AreEqual(original.OutputMaxKw, roundTripped.OutputMaxKw);
            Assert.AreEqual(original.InputEnabled, roundTripped.InputEnabled);
            Assert.AreEqual(original.OutputEnabled, roundTripped.OutputEnabled);
            Assert.AreEqual(original.InputActive, roundTripped.InputActive);
            Assert.AreEqual(original.OutputActive, roundTripped.OutputActive);
            Assert.AreEqual(original.ConnectionStateText, roundTripped.ConnectionStateText);
            Assert.AreEqual(original.AccessGranted, roundTripped.AccessGranted);
            Assert.AreEqual(original.AccessScanning, roundTripped.AccessScanning);
            Assert.AreEqual(original.AccessDenied, roundTripped.AccessDenied);
        }

        [Test]
        public void ApcSnapshotMapper_MapsOverloadState()
        {
            ApcInterfaceSnapshot snapshot = CreateApcSnapshot();
            snapshot.PowerState = (byte)ApcPowerState.Overload;

            ApcInterfaceViewModel model = ApcInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual(ApcPowerState.Overload, model.State);
            Assert.AreEqual("GRID OVERLOAD", model.StatusHeadline);
            Assert.AreEqual(true, model.LightingOn);
            Assert.AreEqual(true, model.EquipmentOn);
            Assert.AreEqual(false, model.EnvironmentOn);
            Assert.AreEqual(1, model.Diagnostics.Count);
        }

        [Test]
        public void SmesSnapshotMapper_MapsFaultState()
        {
            SmesInterfaceSnapshot snapshot = CreateSmesSnapshot();
            snapshot.PowerState = (byte)SmesPowerState.Fault;

            SmesInterfaceViewModel model = SmesInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual(SmesPowerState.Fault, model.State);
            Assert.AreEqual("FAULT", model.StatusBadgeText);
            Assert.AreEqual("SMES Offline", model.ExteriorStatusWord);
        }

        [Test]
        public void VendingInterfaceSnapshotSerializer_RoundTrips()
        {
            VendingInterfaceSnapshot original = CreateVendingSnapshot();

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteVendingInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            VendingInterfaceSnapshot roundTripped = reader.ReadVendingInterfaceSnapshot();

            Assert.AreEqual(original.MachineObjectId, roundTripped.MachineObjectId);
            Assert.AreEqual(original.InterfaceId, roundTripped.InterfaceId);
            Assert.AreEqual(original.Title, roundTripped.Title);
            Assert.AreEqual(original.IdScanned, roundTripped.IdScanned);
            Assert.AreEqual(original.Scanning, roundTripped.Scanning);
            Assert.AreEqual(original.ProductCount, roundTripped.ProductCount);
            Assert.AreEqual(original.Product0.Name, roundTripped.Product0.Name);
            Assert.AreEqual(original.Product0.Stock, roundTripped.Product0.Stock);
            Assert.AreEqual(original.Product1.RequiresId, roundTripped.Product1.RequiresId);
            Assert.AreEqual(original.TrayItemCount, roundTripped.TrayItemCount);
            Assert.AreEqual(original.Tray0.Name, roundTripped.Tray0.Name);
            Assert.AreEqual(original.LogEntryCount, roundTripped.LogEntryCount);
            Assert.AreEqual(original.Log0, roundTripped.Log0);
        }

        [Test]
        public void VendingSnapshotMapper_MapsTrayAndStock()
        {
            VendingInterfaceSnapshot snapshot = CreateVendingSnapshot();
            VendingInterfaceViewModel model = VendingInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual(2, model.Products.Count);
            Assert.AreEqual("Ration Bar", model.Products[0].Name);
            Assert.AreEqual(1, model.TrayItems.Count);
            Assert.AreEqual("Space Cola", model.TrayItems[0].Name);
            Assert.AreEqual(1, model.ActionLog.Count);
        }

        [Test]
        public void IdConsoleInterfaceSnapshotSerializer_RoundTrips()
        {
            IdConsoleInterfaceSnapshot original = new()
            {
                MachineObjectId = 9,
                InterfaceId = MachineInterfaceIds.IdConsole,
                Title = "ID CONSOLE",
                Subtitle = "Personnel Access Management",
                ModelLabel = "IDC-1",
                PromptText = "Edit access levels below.",
                EditorUnlocked = true,
                HasTargetCard = true,
                TargetName = "Alice",
                TargetJob = "Engineer",
                TargetAccessMask = (ulong)AccessLevel.Engineering,
                LogEntryCount = 1,
                Log0 = "Granted Engineering on Alice.",
            };

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteIdConsoleInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            IdConsoleInterfaceSnapshot roundTripped = reader.ReadIdConsoleInterfaceSnapshot();

            Assert.AreEqual(original.TargetName, roundTripped.TargetName);
            Assert.AreEqual(original.TargetAccessMask, roundTripped.TargetAccessMask);
            Assert.AreEqual(original.Log0, roundTripped.Log0);
        }

        [Test]
        public void AirAlarmInterfaceSnapshotSerializer_RoundTripsVariableDeviceList()
        {
            AirAlarmInterfaceSnapshot original = new()
            {
                MachineObjectId = 12,
                InterfaceId = MachineInterfaceIds.AirAlarm,
                Title = "AIR ALARM",
                ActiveMode = (byte)AirAlarmPresetMode.Panic,
                HasSample = true,
                PressureKpa = 101.3f,
                OxygenFraction = 0.21f,
                NitrogenFraction = 0.78f,
                CarbonDioxideFraction = 0.01f,
                PlasmaFraction = 0.001f,
                TemperatureKelvin = 294f,
                ConnectedDevices = new System.Collections.Generic.List<AirAlarmDeviceSnapshot>
                {
                    new()
                    {
                        Id = "101",
                        Name = "Vent — North",
                        Kind = (byte)AirAlarmConnectedDeviceKind.Vent,
                        Powered = true,
                        TargetKpa = 101f,
                    },
                    new()
                    {
                        Id = "102",
                        Name = "Scrubber — South",
                        Kind = (byte)AirAlarmConnectedDeviceKind.Scrubber,
                        Powered = false,
                        FilterCo2 = true,
                        FilterPlasma = true,
                    },
                },
            };

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteAirAlarmInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            AirAlarmInterfaceSnapshot roundTripped = reader.ReadAirAlarmInterfaceSnapshot();

            Assert.AreEqual(original.MachineObjectId, roundTripped.MachineObjectId);
            Assert.AreEqual(original.ActiveMode, roundTripped.ActiveMode);
            Assert.AreEqual(original.HasSample, roundTripped.HasSample);
            Assert.AreEqual(original.PlasmaFraction, roundTripped.PlasmaFraction);
            Assert.AreEqual(original.TemperatureKelvin, roundTripped.TemperatureKelvin);
            Assert.AreEqual(2, roundTripped.ConnectedDevices.Count);
            Assert.AreEqual("101", roundTripped.ConnectedDevices[0].Id);
            Assert.AreEqual("Scrubber — South", roundTripped.ConnectedDevices[1].Name);
            Assert.IsTrue(roundTripped.ConnectedDevices[1].FilterPlasma);
        }

        [Test]
        public void AirAlarmSnapshotMapper_AppliesLiveGasTemperatureAndPressureReadings()
        {
            AirAlarmInterfaceSnapshot snapshot = new()
            {
                Scenario = (byte)AirAlarmScenario.Normal,
                HasSample = true,
                PressureKpa = 88.4f,
                TemperatureKelvin = 330f,
                OxygenFraction = 0.19f,
                NitrogenFraction = 0.75f,
                CarbonDioxideFraction = 0.04f,
                PlasmaFraction = 0.02f,
            };

            AirAlarmInterfaceViewModel model = AirAlarmInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual("88.4 kPa", model.PressureText);
            Assert.AreEqual("56.9°C", model.TemperatureText);
            Assert.AreEqual(4, model.GasReadouts.Count);
            Assert.AreEqual(19f, model.GasReadouts[0].Percent, 0.1f);
            Assert.AreEqual(75f, model.GasReadouts[1].Percent, 0.1f);
            Assert.AreEqual(4f, model.GasReadouts[2].Percent, 0.1f);
            Assert.AreEqual(2f, model.GasReadouts[3].Percent, 0.1f);
        }

        [Test]
        public void ScrubberInterfaceSnapshotSerializer_RoundTripsFilterState()
        {
            ScrubberInterfaceSnapshot original = new()
            {
                MachineObjectId = 55,
                InterfaceId = MachineInterfaceIds.Scrubber,
                Title = "SCRUBBER",
                FlowRate = 7,
                FilterO2 = false,
                FilterN2 = true,
                FilterCo2 = true,
                FilterPlasma = true,
                FilterToxins = false,
            };

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteScrubberInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            ScrubberInterfaceSnapshot roundTripped = reader.ReadScrubberInterfaceSnapshot();

            Assert.AreEqual(original.FilterO2, roundTripped.FilterO2);
            Assert.AreEqual(original.FilterPlasma, roundTripped.FilterPlasma);
            Assert.AreEqual(original.FilterToxins, roundTripped.FilterToxins);
            Assert.AreEqual(7, roundTripped.FlowRate);
        }

        private static ApcInterfaceSnapshot CreateApcSnapshot()
        {
            return new ApcInterfaceSnapshot
            {
                MachineObjectId = 42,
                InterfaceId = MachineInterfaceIds.Apc,
                Title = "APC · TEST",
                Channels = ApcControlFlags.Lighting | ApcControlFlags.Equipment,
                PowerState = (byte)ApcPowerState.Nominal,
                GridInputKw = 12.4f,
                LoadOutputKw = 9.8f,
                BatteryCharge = 0.92f,
                BatteryState = (byte)ApcBatteryState.Charged,
                LightingLoadKw = 2.1f,
                EquipmentLoadKw = 5.4f,
                EnvironmentLoadKw = 2.3f,
                MultipleApcsInArea = false,
                AccessGranted = true,
                AccessScanning = false,
                AccessDenied = false,
                DiagnosticCount = 1,
                Diagnostic0 = new ApcDiagnosticSnapshot
                {
                    Glyph = ">",
                    Text = "External power available.",
                    Tone = (byte)StatusTone.Success,
                },
            };
        }

        private static SmesInterfaceSnapshot CreateSmesSnapshot()
        {
            return new SmesInterfaceSnapshot
            {
                MachineObjectId = 84,
                InterfaceId = MachineInterfaceIds.Smes,
                Title = "SMES · TEST",
                PowerState = (byte)SmesPowerState.Nominal,
                ChargePct = 0.72f,
                ChargeTrend = (byte)SmesChargeTrend.Steady,
                InputCurrentKw = 3.2f,
                OutputCurrentKw = 4.1f,
                InputMaxKw = 10f,
                OutputMaxKw = 10f,
                InputEnabled = true,
                OutputEnabled = true,
                InputActive = true,
                OutputActive = true,
                ConnectionStateText = "Grid link nominal — both connections healthy.",
                AccessGranted = false,
                AccessScanning = true,
                AccessDenied = true,
            };
        }

        [Test]
        public void PumpInterfaceSnapshotSerializer_RoundTrips()
        {
            PumpInterfaceSnapshot original = CreatePumpSnapshot();

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WritePumpInterfaceSnapshot(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            PumpInterfaceSnapshot roundTripped = reader.ReadPumpInterfaceSnapshot();

            Assert.AreEqual(original.MachineObjectId, roundTripped.MachineObjectId);
            Assert.AreEqual(original.InterfaceId, roundTripped.InterfaceId);
            Assert.AreEqual(original.TargetOutletPressureKpa, roundTripped.TargetOutletPressureKpa);
            Assert.AreEqual(original.InletPressureKpa, roundTripped.InletPressureKpa);
            Assert.AreEqual(original.OutletPressureKpa, roundTripped.OutletPressureKpa);
            Assert.AreEqual(original.FlowMolesPerSecond, roundTripped.FlowMolesPerSecond);
            Assert.AreEqual(original.Scenario, roundTripped.Scenario);
            Assert.AreEqual(original.AccessGranted, roundTripped.AccessGranted);
            Assert.AreEqual(original.AccessScanning, roundTripped.AccessScanning);
            Assert.AreEqual(original.AccessDenied, roundTripped.AccessDenied);
        }

        [Test]
        public void PumpSnapshotMapper_MapsPumpingScenario()
        {
            PumpInterfaceSnapshot snapshot = CreatePumpSnapshot();
            snapshot.Scenario = (byte)PumpScenario.Pumping;
            snapshot.FlowMolesPerSecond = 0.55f;

            PumpInterfaceViewModel model = PumpInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual(PumpScenario.Pumping, model.Scenario);
            Assert.AreEqual("PUMPING", model.StatusBadgeText);
            Assert.AreEqual("Flowing — 12.3 L/s", model.FlowStatusText);
        }

        private static PumpInterfaceSnapshot CreatePumpSnapshot()
        {
            return new PumpInterfaceSnapshot
            {
                MachineObjectId = 210,
                InterfaceId = MachineInterfaceIds.Pump,
                Title = "PUMP · TEST",
                ModelLabel = "PMP-22 · pipe pump unit",
                DeviceTitle = "PMP-22",
                Subtitle = "Engineering Bay 3 — Pipe Pump",
                PowerOk = true,
                Powered = true,
                Connected = true,
                Scenario = (byte)PumpScenario.Idle,
                AccessGranted = true,
                AccessScanning = false,
                AccessDenied = false,
                TargetOutletPressureKpa = 4500,
                InletPressureKpa = 101.3f,
                OutletPressureKpa = 4487.6f,
                FlowMolesPerSecond = 0.55f,
            };
        }

        private static VendingInterfaceSnapshot CreateVendingSnapshot()
        {
            return new VendingInterfaceSnapshot
            {
                MachineObjectId = 126,
                InterfaceId = MachineInterfaceIds.Vending,
                Title = "GALLEY VENDOMAT",
                Subtitle = "Crew Mess — Sundries Dispenser",
                ModelLabel = "VND-7 · ration & sundries dispenser",
                ConnectionStatus = "WIRED · VEND BUS · PORT J1",
                StockedReadout = "2 of 2 items stocked",
                PowerOk = true,
                IdScanned = true,
                Scanning = false,
                ProductCount = 2,
                Product0 = new VendingProductSnapshot { Name = "Ration Bar", Stock = 4, RequiresId = false },
                Product1 = new VendingProductSnapshot { Name = "Water Pouch", Stock = 0, RequiresId = true },
                TrayItemCount = 1,
                Tray0 = new VendingTrayItemSnapshot { Name = "Space Cola" },
                LogEntryCount = 1,
                Log0 = "[07:45] Space Cola dispensed to tray",
            };
        }
    }
}
