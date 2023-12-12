﻿using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    public class BasicPowerConsumer : BasicElectricDevice, IPowerConsumer
    {
        [SerializeField]
        private float _powerConsumption = 1f;

        [SyncVar(OnChange = nameof(SyncPowerStatus))]
        private PowerStatus _powerStatus;
        public float PowerNeeded => _powerConsumption;

        public event EventHandler<PowerStatus> OnPowerStatusUpdated;

        public PowerStatus PowerStatus { get => _powerStatus; set => _powerStatus = value; }

        private void SyncPowerStatus(PowerStatus oldValue, PowerStatus newValue, bool asServer)
        {
            OnPowerStatusUpdated.Invoke(this, newValue);
        }

        private void OnElectricitySystemSetup()
        {
            ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
            electricitySystem.AddElectricalElement(this);
        }
    }
}