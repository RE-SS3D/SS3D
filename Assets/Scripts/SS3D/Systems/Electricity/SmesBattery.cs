﻿using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Script for SMES battery, mostly to handle displaying visual indicators on the SMES models.
    /// </summary>
    public class SmesBattery : BasicBattery
    {
        [SerializeField]
        private SkinnedMeshRenderer SmesSkinnedMesh;

        // Bunch of blend shape indexes.

        private const int ChargeblendIndex = 0;

        private const int OnBlendIndex = 12;

        private const int OffBlendIndex = 13;



        private float _previousPowerStored = 0f;

        private int _currentLightOutput = 0;

        private int _lightOutputTarget = 0;

        /// <summary>
        /// How much tick before updating the output lights.
        /// </summary>
        [SerializeField]
        private int _updateLightPeriod = 3;

        private int _updateCount = 0;


        public override void OnStartClient()
        {
            base.OnStartClient();
            GetComponent<GenericToggleInteractionTarget>().OnToggle += HandleBatteryToggle;
            HandleBatteryToggle(_isOn);

            Subsystems.Get<ElectricitySystem>().OnTick += HandleTick;
        }

        [Client]
        private void HandleTick()
        {
            AdjustBatteryLevel();
            AdjustBatteryOutput();
            AdjustBatteryInput();
            _previousPowerStored = StoredPower;
            _updateCount++;
        }

        /// <summary>
        /// Adjust the battery level, the liquid thingy going up and down, depending on the amount of stored power.
        /// </summary>
        [Client]
        private void AdjustBatteryLevel()
        {
            float chargeLevelNormalized = StoredPower / MaxCapacity;
            SmesSkinnedMesh.SetBlendShapeWeight(ChargeblendIndex, chargeLevelNormalized*100);
        }

        /// <summary>
        /// Just turn on the battery input light, if power was added. Turn it off if no power added, or power removed.
        /// </summary>
        [Client]
        private void AdjustBatteryInput()
        {
            float powerAdded = Mathf.Max(StoredPower - _previousPowerStored, 0f);

            if(powerAdded > 0f)
            {
                SmesSkinnedMesh.SetBlendShapeWeight(11, 100f);
            }
            else
            {
                SmesSkinnedMesh.SetBlendShapeWeight(11, 0f);
            }
        }

        /// <summary>
        /// Set the vertical line of output lights so that, at each update, the bar go up and down toward the light it should reached.
        /// Do nothing if the target light is reached.
        /// </summary>
        [Client]
        private void AdjustBatteryOutput()
        {
            ComputeLightOutputTarget();

            if (_updateCount != _updateLightPeriod) return;

            _updateCount = 0;

            if (_currentLightOutput < _lightOutputTarget)
            {
                _currentLightOutput += 1;
                SmesSkinnedMesh.SetBlendShapeWeight(_currentLightOutput, 100);
            }
            else if(_currentLightOutput > _lightOutputTarget)
            {
                SmesSkinnedMesh.SetBlendShapeWeight(_currentLightOutput, 0);
                _currentLightOutput -= 1;
            }      
        }

        /// <summary>
        /// Index of the light that should be turned on. Can be 0 and in that case no light should be on.
        /// Assumes the index of blendshapes for the output lights are from 1 to 11.
        /// </summary>
        [Client]
        private void ComputeLightOutputTarget()
        {
            float powerRemoved = Mathf.Max(_previousPowerStored - StoredPower, 0f);
            float relativeRate = Mathf.Floor((powerRemoved / MaxPowerRate) * 10);

            _lightOutputTarget = (int)relativeRate; 
        }

        /// <summary>
        /// Return the state of the SMES battery.
        /// </summary>
        /// <returns> The state of the battery. </returns>
        public bool GetState()
        {
            return IsOn;
        }

        /// <summary>
        /// Called when the SMES battery is toggled on or off.
        /// </summary>
        /// <param name="toggle"> True if the battery is on.</param>
        public void HandleBatteryToggle(bool toggle)
        {
            _isOn = toggle;

            
        }

        protected override void SyncEnabled(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer) return;

            if (newValue)
            {
                SmesSkinnedMesh.SetBlendShapeWeight(OnBlendIndex, 100);
                SmesSkinnedMesh.SetBlendShapeWeight(OffBlendIndex, 0);
            }
            else
            {
                SmesSkinnedMesh.SetBlendShapeWeight(OnBlendIndex, 0);
                SmesSkinnedMesh.SetBlendShapeWeight(OffBlendIndex, 100);
            }

        }
    }
}