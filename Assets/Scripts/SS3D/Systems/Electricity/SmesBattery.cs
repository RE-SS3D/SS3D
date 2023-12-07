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

        public override void OnStartClient()
        {
            base.OnStartClient();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            AdjustVisualDisplay();
        }


        private void AdjustVisualDisplay()
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


    }
}
