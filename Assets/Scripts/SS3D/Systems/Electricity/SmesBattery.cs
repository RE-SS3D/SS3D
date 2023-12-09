using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace System.Electricity
{
    public class SmesBattery : BasicBattery
    {
        [SerializeField]
        private SkinnedMeshRenderer SmesSkinnedMesh;

        private const int ChargeblendIndex = 0;

        private const int OnBlendIndex = 12;

        private const int OffBlendIndex = 13;



        private float _previousPowerStored = 0f;

        private int _currentLightOutput = 0;

        private int _lightOutputTarget = 0;

        [SerializeField]
        private int _updateLightPeriod = 25;

        private int _updateCount = 0;



        public override void OnStartClient()
        {
            base.OnStartClient();
            GetComponent<GenericToggleInteractionTarget>().OnToggle += HandleBatteryToggle;
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            AdjustBatteryLevel();
            AdjustBatteryOutput();
            AdjustBatteryInput();
            _previousPowerStored = StoredPower;
        }


        private void AdjustBatteryLevel()
        {
            float chargeLevelNormalized = StoredPower / MaxCapacity;
            SmesSkinnedMesh.SetBlendShapeWeight(ChargeblendIndex, chargeLevelNormalized*100);
        }

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
        private void ComputeLightOutputTarget()
        {
            float powerRemoved = Mathf.Max(_previousPowerStored - StoredPower, 0f);
            float relativeRate = Mathf.Floor((powerRemoved / MaxPowerRate) * 10);

            _lightOutputTarget = (int)relativeRate; 
        }

        public bool GetState()
        {
            return IsOn;
        }

        public void HandleBatteryToggle(bool toggle)
        {
            _isOn = toggle;

            if(_isOn)
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
