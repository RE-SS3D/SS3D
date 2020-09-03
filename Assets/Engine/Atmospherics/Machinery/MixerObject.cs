using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class MixerObject : PipeGeneric, IAtmosLoop, IInteractionTarget
    {
        public float InputOneAmount = 50f;
        public float MaxPressure = 4500f;
        public float TargetPressure = 101f;
        public bool mixerActive = false;

        private float _targetPressure;
        private float ratioOnetoTwo;
        

        public void Initialize()
        {
            SetAtmosNeighbours();
        }

        void Start()
        {
            ratioOnetoTwo = InputOneAmount / 100f;
        }

        public void Update()
        {
            if (_targetPressure != TargetPressure)
            {
                _targetPressure = Mathf.Clamp(TargetPressure, 0, MaxPressure);
                TargetPressure = _targetPressure;
            }
        }

        public void SetActive(bool mixerActive)
        {
            this.mixerActive = mixerActive;
        }

        public void Step()
        {
            if (mixerActive)
            {
                ratioOnetoTwo = InputOneAmount / 100f;

                PipeObject input1 = atmosNeighbours[0];
                PipeObject input2 = atmosNeighbours[3];
                PipeObject output = atmosNeighbours[1];

                if (!input1 || !input2 || !output)
                    return;

                float[] inputGasses1 = input1.GetAtmosContainer().GetGasses();
                float[] inputGasses2 = input2.GetAtmosContainer().GetGasses();
                float[] outputGasses = output.GetAtmosContainer().GetGasses();

                if (input1.GetTotalMoles() <= 1f || input2.GetTotalMoles() <= 1f)
                    return;

                if (output.GetPressure() <= _targetPressure)
                {
                    float totalMoles = output.GetTotalMoles();

                    // Calculate necessary moles to transfer using PV=nRT
                    float pressureDifference = _targetPressure - output.GetPressure();
                    float transferMoles = pressureDifference * 1000 * output.volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                    // We can not transfer more moles than the machinery allows
                    transferMoles = Mathf.Min(Gas.maxMoleTransfer, transferMoles);

                    float transfer_moles1 = ratioOnetoTwo * transferMoles;
                    float transfer_moles2 = (1f - ratioOnetoTwo) * transferMoles;


                    // We can't transfer more moles than there are
                    float inputMoles1 = input1.GetTotalMoles();
                    float inputMoles2 = input2.GetTotalMoles();

                    // If one of the inputs didn't contain enough gas, scale the other down
                    if (transfer_moles1 > input1.GetTotalMoles())
                    {
                        transfer_moles2 = input1.GetTotalMoles() * (1 / ratioOnetoTwo) * (1 - ratioOnetoTwo);
                        transfer_moles1 = input1.GetTotalMoles();
                    }
                    if (transfer_moles2 > input2.GetTotalMoles())
                    {
                        transfer_moles1 = input2.GetTotalMoles() * (1 / (1 - ratioOnetoTwo)) * ratioOnetoTwo;
                        transfer_moles2 = input2.GetTotalMoles();
                    }

                    if (transfer_moles1 > inputMoles1 || transfer_moles2 > inputMoles2)
                        Debug.LogError("More gas to be transfered than possible");

                    for (int i = 0; i < Gas.numOfGases; i++)
                    {
                        // Input 1 and input 2
                        float molePerGas1 = (inputGasses1[i] / inputMoles1) * transfer_moles1;
                        float molePerGas2 = (inputGasses2[i] / inputMoles2) * transfer_moles2;
                        if (inputGasses1[i] >= molePerGas1 && inputGasses1[i] > 0.1f)
                        {
                            input1.GetAtmosContainer().RemoveGas(i, molePerGas1);
                            output.GetAtmosContainer().AddGas(i, molePerGas1);
                        }

                        if (inputGasses2[i] >= molePerGas2 && inputGasses2[i] > 0.1f)
                        {
                            input2.GetAtmosContainer().RemoveGas(i, molePerGas2);
                            output.GetAtmosContainer().AddGas(i, molePerGas2);
                        }
                    }

                    input1.SetStateActive();
                    input2.SetStateActive();
                    output.SetStateActive();
                }
            }
        }

        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = mixerActive ? "Stop mixer" : "Start mixer", Interact = MixerInteract, RangeCheck = true
                }
            };
        }

        private void MixerInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            SetActive(!mixerActive);
        }
    }
}