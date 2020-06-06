using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class MixerObject : MonoBehaviour, IAtmosLoop, IInteractionTarget
    {
        public float InputOneAmount = 50f;
        public float TargetPressure = 300f;
        public bool mixerActive = false;

        private float ratioOnetoTwo;
        private const float stepsToEqualize = 10f;

        private TileObject[] tileNeighbours = { null, null, null, null };
        private PipeObject[] atmosNeighbours = { null, null, null, null };

        

        public void Initialize()
        {
            SetAtmosNeighbours();
        }

        public void SetAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                    atmosNeighbours[i] = tile.transform.GetComponentInChildren<PipeObject>();
                i++;
            }
        }

        public void SetTileNeighbour(TileObject tile, int index)
        {
            tileNeighbours[index] = tile;
        }

        public void SetActive(bool mixerActive)
        {
            this.mixerActive = mixerActive;
        }

        public void Step()
        {
            if (mixerActive)
            {
                ratioOnetoTwo = InputOneAmount / 100;

                PipeObject input1 = atmosNeighbours[0];
                PipeObject input2 = atmosNeighbours[3];
                PipeObject output = atmosNeighbours[1];

                if (!input1 || !input2 || !output)
                    return;

                float[] inputGasses1 = input1.GetAtmosContainer().GetGasses();
                float[] inputGasses2 = input2.GetAtmosContainer().GetGasses();
                float[] outputGasses = output.GetAtmosContainer().GetGasses();

                if (input1.GetTotalMoles() == 0 || input2.GetTotalMoles() == 0)
                    return;

                if (output.GetPressure() <= TargetPressure - 0.1f)
                {
                    float totalMoles = output.GetTotalMoles();

                    // Calculate necessary moles to transfer using PV=nRT
                    float pressureDifference = TargetPressure - output.GetPressure();
                    float transferMoles = pressureDifference * 1000 * output.volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                    // Reach our target pressure in N steps
                    transferMoles = transferMoles / stepsToEqualize;

                    // transfer_moles1 = air1.temperature ? node1_concentration * general_transfer / air1.temperature : 0
                    float transfer_moles1 = ratioOnetoTwo * transferMoles; // / input1.GetTemperature();
                    float transfer_moles2 = (1 - ratioOnetoTwo) * transferMoles; // / input2.GetTemperature();


                    // We can't transfer more moles than there are
                    transfer_moles1 = Mathf.Min(transfer_moles1, input1.GetTotalMoles());
                    transfer_moles2 = Mathf.Min(transfer_moles2, input2.GetTotalMoles());
                    float inputMoles1 = input1.GetTotalMoles();
                    float inputMoles2 = input2.GetTotalMoles();

                    // If one of the inputs didn't contain enough gas. Scale the other down
                    if (transfer_moles1 == input1.GetTotalMoles())
                    {
                        //transfer_moles2 = transfer_moles1 * (1 / ratioOnetoTwo) * (1 - ratioOnetoTwo);
                        //inputMoles2 = 
                        return;
                    }
                    if (transfer_moles2 == input2.GetTotalMoles())
                    {
                        //transfer_moles1 = transfer_moles2 * (1 / (1 - ratioOnetoTwo)) * ratioOnetoTwo;
                        return;
                    }

                    bool set1 = false;
                    bool set2 = false;

                    for (int i = 0; i < Gas.numOfGases; i++)
                    {
                        // Input 1 and input 2
                        float molePerGas1 = (inputGasses1[i] / inputMoles1) * transfer_moles1;
                        float molePerGas2 = (inputGasses2[i] / inputMoles2) * transfer_moles2;
                        if (inputGasses1[i] >= molePerGas1 && inputGasses1[i] > 0.1f)
                        {
                            inputGasses1[i] -= molePerGas1;
                            outputGasses[i] += molePerGas1;
                            set1 = true;
                        }

                        if (inputGasses2[i] >= molePerGas2 && inputGasses2[i] > 0.1f)
                        {
                            inputGasses2[i] -= molePerGas2;
                            outputGasses[i] += molePerGas2;
                            set2 = true;
                        }
                    }

                    if (set1 != set2)
                        Debug.LogError("Input 1/2 difference");

                    float toRatioOne = outputGasses[1] * (1 / ratioOnetoTwo);
                    float toRatioTwo = outputGasses[0] * (1 / (1 - ratioOnetoTwo));

                    if (toRatioOne  != toRatioTwo)
                        Debug.LogError("Content bug...");

                    input1.SetStateActive();
                    input2.SetStateActive();
                    output.SetStateActive();
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            ratioOnetoTwo = InputOneAmount / 100;
        }

        // Update is called once per frame
        void Update()
        {

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