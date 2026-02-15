using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using UnityEngine;
using UnityEngine.Serialization;

namespace System.Electricity
{
    /// <summary>
    /// Script for SMES battery, mostly to handle displaying visual indicators on the SMES models.
    /// </summary>
    public class SmesBattery : BasicBattery
    {
        [FormerlySerializedAs("SmesSkinnedMesh")]
        [SerializeField]
        private SkinnedMeshRenderer _smesSkinnedMesh;

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

            SubSystems.Get<ElectricitySubSystem>().OnTick += HandleTick;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            SubSystems.Get<ElectricitySubSystem>().OnTick -= HandleTick;
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
            _smesSkinnedMesh.SetBlendShapeWeight(ChargeblendIndex, chargeLevelNormalized*100);
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
                _smesSkinnedMesh.SetBlendShapeWeight(11, 100f);
            }
            else
            {
                _smesSkinnedMesh.SetBlendShapeWeight(11, 0f);
            }
        }

        /// <summary>
        /// Set the vertical line of output lights so that, at each update, the bar goes up and down toward the light it should reached.
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
                _smesSkinnedMesh.SetBlendShapeWeight(_currentLightOutput, 100);
            }
            else if(_currentLightOutput > _lightOutputTarget)
            {
                _smesSkinnedMesh.SetBlendShapeWeight(_currentLightOutput, 0);
                _currentLightOutput -= 1;
            }      
        }

        /// <summary>
        /// Calculate index of the light that should be turned on. Can be 0 and in that case no light should be on.
        /// Assumes the index of blendshapes for the output lights are from 1 to 11.
        /// </summary>
        [Client]
        private void ComputeLightOutputTarget()
        {
            float powerRemoved = Mathf.Max(_previousPowerStored - StoredPower, 0f);
            float relativeRate = Mathf.Floor(10 * powerRemoved / MaxPowerRate);

            _lightOutputTarget = (int)relativeRate; 
        }

        /// <summary>
        /// Called when the SMES battery is toggled on or off.
        /// </summary>
        /// <param name="toggle"> True if the battery is on.</param>
        private void HandleBatteryToggle(bool toggle)
        {
            _isOn = toggle;
        }

        protected override void HandleSyncEnabled(bool oldValue, bool newValue, bool asServer)
        {
            if (asServer) return;

            if (newValue)
            {
                _smesSkinnedMesh.SetBlendShapeWeight(OnBlendIndex, 100);
                _smesSkinnedMesh.SetBlendShapeWeight(OffBlendIndex, 0);
            }
            else
            {
                _smesSkinnedMesh.SetBlendShapeWeight(OnBlendIndex, 0);
                _smesSkinnedMesh.SetBlendShapeWeight(OffBlendIndex, 100);
            }
        }
    }
}
