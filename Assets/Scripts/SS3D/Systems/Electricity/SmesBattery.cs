using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
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

        private const string ChargeblendShapeName = "Charge";

        private const string OutputblendShapeName = "OutputMeter";

        private float _previousPowerStored = 0f;

        private int _currentLightOutput = 0;

        private int _lightOutputTarget = 0;

        [SerializeField]
        private int _updateLightPeriod = 25;

        private int _updateCount = 0;



        public override void OnStartClient()
        {
            base.OnStartClient();
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
            int blendShapeIndex = SmesSkinnedMesh.sharedMesh.GetBlendShapeIndex(ChargeblendShapeName);

            float chargeLevelNormalized = StoredPower / MaxCapacity;

            if (blendShapeIndex != -1)
            {
                SmesSkinnedMesh.SetBlendShapeWeight(blendShapeIndex, chargeLevelNormalized*100);
            }
            else
            {
                Debug.LogError("Blend shape " + ChargeblendShapeName + " not found.");
            }
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
                SmesSkinnedMesh.SetBlendShapeWeight(0, 100f);
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
        /// </summary>
        private void ComputeLightOutputTarget()
        {
            float powerRemoved = Mathf.Max(_previousPowerStored - StoredPower, 0f);
            float relativeRate = Mathf.Floor((powerRemoved / MaxPowerRate) * 10);

            _lightOutputTarget = (int)relativeRate; // plus one because the blend shapes for light output start at index one.
        }


    }
}
