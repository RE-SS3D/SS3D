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

        private void AdjustBatteryOutput()
        {
            ComputeLightOutputTarget();
            _previousPowerStored = StoredPower;
            _updateCount++;

            if (_updateCount != _updateLightPeriod) return;

            _updateCount = 0;

            Debug.Log("target = " + _lightOutputTarget);
            Debug.Log("current = " + _currentLightOutput);

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
        /// Index of the light that should be turned on.
        /// </summary>
        private void ComputeLightOutputTarget()
        {
            float powerRemoved = Mathf.Max(_previousPowerStored - StoredPower, 0f);
            float relativeRate = Mathf.Floor((powerRemoved / MaxPowerRate) * 10);

            _lightOutputTarget = (int)relativeRate; // plus one because the blend shapes for light output start at index one.
        }


    }
}
