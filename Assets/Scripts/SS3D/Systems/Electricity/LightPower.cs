using PlasticGui.WorkspaceWindow.PendingChanges;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    public class LightPower : MonoBehaviour
    {
        [SerializeField]
        private BasicPowerConsumer _consumer;

        [SerializeField]
        private Light _light;

        private float poweredIntensity;


        void Start()
        {
            _consumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            poweredIntensity = _light.intensity;

            TurnLightOff();
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            UpdateLights(newStatus);
        }

        private void UpdateLights(PowerStatus powerStatus)
        {
            if (powerStatus == PowerStatus.Powered) TurnLightOn();
            else TurnLightOff();
        }

        private void TurnLightOn()
        {
            _light.intensity = poweredIntensity;
        }

        private void TurnLightOff()
        {
            _light.intensity = 0;
        }
    }
}
