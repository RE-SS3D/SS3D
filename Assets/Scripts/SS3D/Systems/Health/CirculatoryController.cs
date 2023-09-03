using Newtonsoft.Json.Bson;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Codice.Client.Common.WebApi.WebApiEndpoints;
using static Codice.CM.Common.CmCallContext;

public class CirculatoryController : NetworkActor
{
    [SerializeField]
    private Heart _heart;

    [SerializeField]
    private SubstanceContainer _container;

    public SubstanceContainer Container => _container;

    public override void OnStartServer()
    {
	    base.OnStartServer();
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return null;
        yield return null;

        _heart.OnPulse += HandleHeartPulse;

        List<BodyPart> connectedToHeart = GetAllBodyPartAttachedToHeart();
        // Around 5 liters of blood in a full adult human
        _container.ChangeVolume((float)connectedToHeart.Sum(x => x.Volume) / 12f);
    }

    public void HandleHeartPulse(object sender, EventArgs args)
    {
        List<BodyPart> connectedToHeart = GetAllBodyPartAttachedToHeart();
        _container.ChangeVolume((float) connectedToHeart.Sum(x => x.Volume) / 12f);
        SendOxygen(connectedToHeart);
        Bleed(connectedToHeart);
    }

    private void Bleed(List<BodyPart> connectedToHeart)
    {
        foreach (BodyPart part in connectedToHeart)
        {
            var veins = (CirculatoryLayer) part.GetBodyLayer<CirculatoryLayer>();
            veins.Bleed();
        }
    }

    private void SendOxygen(List<BodyPart> connectedToHeart)
    {
        double availableOxygen = AvailableOxygen();

        float[] oxygenNeededForEachpart = ComputeIndividualNeeds(connectedToHeart);
        float sumNeeded = oxygenNeededForEachpart.Sum();
        int i = 0;

        SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
        Substance oxygen = registry.FromType(SubstanceType.Oxygen);

        foreach (BodyPart part in connectedToHeart)
        {
            var veins = (CirculatoryLayer)part.GetBodyLayer<CirculatoryLayer>();
            double proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
            veins.ReceiveOxygen(proportionAvailable*availableOxygen);
            i++;
        }
    }

    /// <summary>
    /// Return the amount of oxygen the circulatory system can send to organs.
    /// </summary>
    private double AvailableOxygen()
    {
        SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
        Substance blood = registry.FromType(SubstanceType.Blood);

        Substance oxygen = registry.FromType(SubstanceType.Oxygen);

        float bloodQuantity = _container.GetSubstanceVolume(blood);

        float healthyBloodQuantity = (float) 0.8 * _container.Volume;

        double oxygenMoles = _container.GetSubstanceQuantity(oxygen);

        return bloodQuantity > healthyBloodQuantity ? oxygenMoles : (bloodQuantity / healthyBloodQuantity) * oxygenMoles;
    }

    private List<BodyPart> GetAllBodyPartAttachedToHeart()
    {
        List<BodyPart> connectedToHeart = new List<BodyPart> { _heart };

        if (_heart.IsInsideBodyPart)
        {
            BodyPart heartContainer = _heart.ExternalBodyPart;
            GetAllBodyPartAttachedToHeartRecursion(connectedToHeart, heartContainer);
        }
        return connectedToHeart;
    }

    private void GetAllBodyPartAttachedToHeartRecursion(List<BodyPart> connectedToHeart, BodyPart current)
    {
            if (current.ContainsLayer(BodyLayerType.Circulatory))
            {
                connectedToHeart.Add(current);
            }

            if (current.HasInternalBodyPart)
            {
                foreach (var part in current.InternalBodyParts)
                {
                    connectedToHeart.Add(part);
                }
            }

            foreach (BodyPart bodyPart in current.ChildBodyParts.Where(x => x.ContainsLayer(BodyLayerType.Circulatory)))
            {
                GetAllBodyPartAttachedToHeartRecursion(connectedToHeart, bodyPart);
            }
    }

    private float[] ComputeIndividualNeeds(List<BodyPart> connectedToHeart)
    {
        float[] oxygenNeededForEachpart = new float[connectedToHeart.Count];
        int i = 0;
        foreach(BodyPart bodyPart in connectedToHeart)
        {
            var circulatory = (CirculatoryLayer) bodyPart.GetBodyLayer<CirculatoryLayer>();
            oxygenNeededForEachpart[i] = (float) circulatory.OxygenNeeded;
            i++;
        }
        return oxygenNeededForEachpart;
    }


}
