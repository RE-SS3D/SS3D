using TMPro;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class MixerView : MonoBehaviour
    {
        private MixerAtmosObject _mixerAtmosObject;

        [SerializeField]
        private SwitchButton _turnOn;

        [SerializeField]
        private SpaceSlider _sliderFirstInput;

        [SerializeField]
        private SpaceSlider _sliderTargetPressure;

        [SerializeField]
        private TextMeshProUGUI _displayFirstInput;

        [SerializeField]
        private TextMeshProUGUI _displayTargetPressure;

        public void Initialize(MixerAtmosObject mixerAtmosObject)
        {
            _mixerAtmosObject = mixerAtmosObject;
            _turnOn.Switch += isOn => _mixerAtmosObject.SetMixerActive(isOn);
            _sliderFirstInput.ValueTickUpdated += UpdateMixerFirstInputAmount;
            _sliderTargetPressure.ValueTickUpdated += UpdateMixerTargetPressure;
            _mixerAtmosObject.OnUpdateMixerFirstInputAmount += amount => UpdateVisualFirstInputAmount(amount, false);
            _mixerAtmosObject.OnUpdateTargetPressure += amount => UpdateVisualTargetPressure(amount, false);
        }

        private void UpdateMixerFirstInputAmount(float amount)
        {
            _mixerAtmosObject.SetFirstInput(amount);
            UpdateVisualFirstInputAmount(amount, true);
        }

        private void UpdateMixerTargetPressure(float amount)
        {
            _mixerAtmosObject.SetTargetPressure(amount);
            UpdateVisualTargetPressure(amount, true);
        }

        private void UpdateVisualFirstInputAmount(float amount, bool fromUI)
        {
            if (!fromUI && _sliderFirstInput.Pressed)
            {
                return;
            }

            _displayFirstInput.text = amount.ToString();
            _sliderFirstInput.value = amount;
        }

        private void UpdateVisualTargetPressure(float amount, bool fromUI)
        {
            if (!fromUI && _sliderTargetPressure.Pressed)
            {
                return;
            }

            _displayTargetPressure.text = amount.ToString();
            _sliderTargetPressure.value = amount;
        }
    }
}
