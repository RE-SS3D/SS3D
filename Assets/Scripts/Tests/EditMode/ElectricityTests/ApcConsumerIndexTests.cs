using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Unit coverage for APC consumer-list helpers used by the electricity tick index path.
    /// </summary>
    public class ApcConsumerIndexTests
    {
        [Test]
        public void GetConsumersForApc_EmptyRegistered_ReturnsEmpty()
        {
            TestApcStorage apc = new TestApcStorage(5f, 5f, 5f);
            List<IPowerConsumer> result = AreaApcPowerDistribution.GetConsumersForApc(
                apc,
                System.Array.Empty<IPowerConsumer>());

            Assert.IsEmpty(result);
        }

        [Test]
        public void GetActiveConsumers_FiltersDisabledChannels()
        {
            TestConsumer lighting = new TestConsumer(1f, PowerChannel.Lighting);
            TestConsumer equipment = new TestConsumer(2f, PowerChannel.Equipment);

            List<IPowerConsumer> active = AreaApcPowerDistribution.GetActiveConsumers(
                new[] { lighting, equipment },
                ApcControlFlags.Lighting);

            Assert.AreEqual(1, active.Count);
            Assert.AreSame(lighting, active[0]);
        }

        [Test]
        public void SumPowerNeeded_SumsListWithoutLinq()
        {
            float sum = AreaApcPowerDistribution.SumPowerNeeded(new IPowerConsumer[]
            {
                new TestConsumer(1.5f, PowerChannel.Lighting),
                new TestConsumer(2.5f, PowerChannel.Equipment),
            });

            Assert.AreEqual(4f, sum);
        }

        [Test]
        public void IsAreaScopedConsumer_NonDeviceConsumer_IsFalse()
        {
            Assert.IsFalse(AreaApcPowerDistribution.IsAreaScopedConsumer(new TestConsumer(1f, PowerChannel.Equipment)));
        }

        [Test]
        public void PowerAreaConsumers_AssignsFinalStatusOnceWithoutFlicker()
        {
            CountingConsumer lighting = new CountingConsumer(1f, PowerChannel.Lighting, PowerStatus.Powered);
            CountingConsumer equipment = new CountingConsumer(1f, PowerChannel.Equipment, PowerStatus.Powered);
            TestApcStorage apc = new TestApcStorage(storedEnergyKwh: 0f, maxCapacityKwh: 5f, maxDischargeRateKw: 0f);

            AreaApcPowerDistribution.PowerAreaConsumers(
                apc,
                apc,
                gridSupplyKw: 1f,
                new IPowerConsumer[] { lighting, equipment },
                new IPowerConsumer[] { lighting, equipment },
                tickSeconds: 3600f);

            Assert.AreEqual(PowerStatus.Powered, lighting.PowerStatus);
            Assert.AreEqual(PowerStatus.Inactive, equipment.PowerStatus);
            // Lighting already Powered — must not write Inactive then Powered (airlock close bug).
            Assert.AreEqual(0, lighting.StatusWriteCount);
            Assert.AreEqual(1, equipment.StatusWriteCount);
        }

        private sealed class TestConsumer : IPowerConsumer
        {
            public TestConsumer(float powerNeeded, PowerChannel channel)
            {
                PowerNeeded = powerNeeded;
                Channel = channel;
            }

            public float PowerNeeded { get; }

            public PowerChannel Channel { get; }

            public PowerStatus PowerStatus { get; set; }

            public PlacedTileObject TileObject => null;
        }

        /// <summary>
        /// Counts PowerStatus writes so tests can catch Inactive→Powered flicker regressions.
        /// </summary>
        private sealed class CountingConsumer : IPowerConsumer
        {
            private PowerStatus _powerStatus;

            public CountingConsumer(float powerNeeded, PowerChannel channel, PowerStatus initialStatus)
            {
                PowerNeeded = powerNeeded;
                Channel = channel;
                _powerStatus = initialStatus;
            }

            public float PowerNeeded { get; }

            public PowerChannel Channel { get; }

            public int StatusWriteCount { get; private set; }

            public PowerStatus PowerStatus
            {
                get => _powerStatus;
                set
                {
                    StatusWriteCount++;
                    _powerStatus = value;
                }
            }

            public PlacedTileObject TileObject => null;
        }

        private sealed class TestApcStorage : IApcChannelSource, IPowerStorage
        {
            public TestApcStorage(float storedEnergyKwh, float maxCapacityKwh, float maxDischargeRateKw, float maxChargeRateKw = -1f)
            {
                StoredEnergyKwh = storedEnergyKwh;
                _maxCapacityKwh = maxCapacityKwh;
                _maxDischargeRateKw = maxDischargeRateKw;
                _maxChargeRateKw = maxChargeRateKw < 0f ? maxDischargeRateKw : maxChargeRateKw;
            }

            private readonly float _maxCapacityKwh;
            private readonly float _maxDischargeRateKw;
            private readonly float _maxChargeRateKw;

            public ApcControlFlags Channels => ApcControlFlags.All;

            public float StoredEnergyKwh { get; set; }

            public float MaxCapacityKwh => _maxCapacityKwh;

            public float RemainingCapacityKwh => _maxCapacityKwh - StoredEnergyKwh;

            public float MaxDischargeRateKw => _maxDischargeRateKw;

            public float MaxChargeRateKw => _maxChargeRateKw;

            public float MaxDeliverableKw(float tickSeconds)
            {
                if (StoredEnergyKwh <= 0f)
                {
                    return 0f;
                }

                return Mathf.Min(_maxDischargeRateKw, ElectricityUnits.KwhToKw(StoredEnergyKwh, tickSeconds));
            }

            public bool IsOn => true;

            public PlacedTileObject TileObject => null;

            public float AddPowerKw(float requestedKw, float tickSeconds)
            {
                if (requestedKw <= 0f || RemainingCapacityKwh <= 0f)
                {
                    return 0f;
                }

                float absorbedKw = Mathf.Min(requestedKw, _maxChargeRateKw);
                float energyToAdd = ElectricityUnits.KwToKwh(absorbedKw, tickSeconds);
                float addedEnergy = Mathf.Min(RemainingCapacityKwh, energyToAdd);
                StoredEnergyKwh += addedEnergy;
                return ElectricityUnits.KwhToKw(addedEnergy, tickSeconds);
            }

            public float RemovePowerKw(float requestedKw, float tickSeconds)
            {
                if (requestedKw <= 0f || StoredEnergyKwh <= 0f)
                {
                    return 0f;
                }

                float deliverableKw = MaxDeliverableKw(tickSeconds);
                float deliveredKw = Mathf.Min(requestedKw, deliverableKw);
                StoredEnergyKwh -= ElectricityUnits.KwToKwh(deliveredKw, tickSeconds);
                return deliveredKw;
            }
        }
    }
}
