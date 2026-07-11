using FishNet.Serializing;
using NUnit.Framework;
using SS3D.Tests;
using SS3D.UI.MachineInterface;
using System;
using System.Electricity;

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
            Assert.AreEqual(original.DiagnosisHint, roundTripped.DiagnosisHint);
            Assert.AreEqual(original.WarningCount, roundTripped.WarningCount);
            Assert.AreEqual(original.Warning0.Glyph, roundTripped.Warning0.Glyph);
            Assert.AreEqual(original.Warning0.Text, roundTripped.Warning0.Text);
            Assert.AreEqual(original.Warning0.Tone, roundTripped.Warning0.Tone);
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
            snapshot.WarningCount = 1;
            snapshot.Warning0 = new ApcDiagnosticSnapshot
            {
                Glyph = "X",
                Text = "Cell bank overheating — output disabled.",
                Tone = (byte)StatusTone.Danger,
            };
            snapshot.DiagnosisHint = "Restore input power before re-enabling output.";

            SmesInterfaceViewModel model = SmesInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual(SmesPowerState.Fault, model.State);
            Assert.AreEqual("FAULT", model.StatusBadgeText);
            Assert.AreEqual("SMES Offline", model.ExteriorStatusWord);
            Assert.AreEqual(1, model.Warnings.Count);
            Assert.AreEqual("Restore input power before re-enabling output.", model.DiagnosisHint);
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
            Assert.AreEqual(original.ProductCount, roundTripped.ProductCount);
            Assert.AreEqual(original.Product0.Name, roundTripped.Product0.Name);
            Assert.AreEqual(original.Product0.Stock, roundTripped.Product0.Stock);
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
                DiagnosisHint = string.Empty,
                WarningCount = 1,
                Warning0 = new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Battery discharge increasing.",
                    Tone = (byte)StatusTone.Warning,
                },
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
                ProductCount = 2,
                Product0 = new VendingProductSnapshot { Name = "Ration Bar", Stock = 4, RequiresId = false },
                Product1 = new VendingProductSnapshot { Name = "Water Pouch", Stock = 0, RequiresId = false },
                TrayItemCount = 1,
                Tray0 = new VendingTrayItemSnapshot { Name = "Space Cola" },
                LogEntryCount = 1,
                Log0 = "[07:45] Space Cola dispensed to tray",
            };
        }
    }
}
