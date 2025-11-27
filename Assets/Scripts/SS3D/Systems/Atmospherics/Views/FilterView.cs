using Coimbra;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Atmospherics
{
    public class FilterView : Actor
    {
        private FilterAtmosObject _filterAtmosObject;

        [SerializeField]
        private SwitchButton _turnOn;

        [SerializeField]
        private SwitchButton _filterOxygen;

        [SerializeField]
        private SwitchButton _filterNitrogen;

        [SerializeField]
        private SwitchButton _filterCarbonDioxyde;

        [SerializeField]
        private SwitchButton _filterPlasma;

        [SerializeField]
        private SpaceSlider _volumeSlider;

        [SerializeField]
        private TextMeshProUGUI _volumeLabel;

        [SerializeField]
        private Button _closeUI;

        public void Initialize(FilterAtmosObject filterAtmosObject)
        {
            _filterAtmosObject = filterAtmosObject;

            _filterOxygen.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.Oxygen);
            _filterNitrogen.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.Nitrogen);
            _filterCarbonDioxyde.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.CarbonDioxide);
            _filterPlasma.Switch += filter => _filterAtmosObject.FilterGas(filter, CoreAtmosGasses.Plasma);
            _turnOn.Switch += isOn => _filterAtmosObject.SetFilterActive(isOn);
            _volumeSlider.ValueTickUpdated += UpdateVolumeTransferred;
            _closeUI.onClick.AddListener(Destroy);

            _filterAtmosObject.OnUpdateFilterGas += UpdateFilterGasVisuals;
            _filterAtmosObject.OnUpdateActive += active => _turnOn.SetState(active, true);
            _filterAtmosObject.OnUpdateFlux += flux => UpdateVolumeTransferredVisuals(flux, false);

            _filterOxygen.SetState(_filterAtmosObject.IsFiltering(CoreAtmosGasses.Oxygen));
            _filterNitrogen.SetState(_filterAtmosObject.IsFiltering(CoreAtmosGasses.Nitrogen));
            _filterCarbonDioxyde.SetState(_filterAtmosObject.IsFiltering(CoreAtmosGasses.CarbonDioxide));
            _filterPlasma.SetState(_filterAtmosObject.IsFiltering(CoreAtmosGasses.Plasma));
            _turnOn.SetState(_filterAtmosObject.FilterActive);
            _volumeSlider.value = _filterAtmosObject.LitersPerSecond;
            _volumeLabel.text = _filterAtmosObject.LitersPerSecond.ToString();
        }

        private void UpdateVolumeTransferred(float volume)
        {
            _filterAtmosObject.SetFlux(volume);
            UpdateVolumeTransferredVisuals(volume, true);
        }

        private void UpdateVolumeTransferredVisuals(float volume, bool fromUI)
        {
            if (!fromUI && _volumeSlider.Pressed)
            {
                return;
            }

            _volumeLabel.text = volume.ToString();
            _volumeSlider.value = volume;
        }

        private void Destroy()
        {
            gameObject.Dispose(true);
        }

        private void UpdateFilterGasVisuals(bool isFiltering, CoreAtmosGasses gas)
        {
            switch (gas)
            {
                case CoreAtmosGasses.Nitrogen:
                {
                    _filterNitrogen.SetState(isFiltering, true);
                    break;
                }

                case CoreAtmosGasses.Oxygen:
                {
                    _filterOxygen.SetState(isFiltering, true);
                    break;
                }

                case CoreAtmosGasses.CarbonDioxide:
                {
                    _filterCarbonDioxyde.SetState(isFiltering, true);
                    break;
                }

                case CoreAtmosGasses.Plasma:
                {
                    _filterPlasma.SetState(isFiltering, true);
                    break;
                }
            }
        }
    }
}
