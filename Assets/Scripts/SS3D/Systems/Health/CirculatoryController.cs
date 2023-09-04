using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Substances;
using SS3D.Systems.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CirculatoryController : NetworkActor
{
    [SerializeField]
    private Heart _heart;

    [SerializeField]
    private SubstanceContainer _container;

    public SubstanceContainer Container => _container;

    public enum BreathingState
    {
        Nice,
        Difficult,
        Suffocating
    }

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

        UpdateVolume();
    }

    public BreathingState breathing;

    /// <summary>
    /// Should be called when a body part is disconnected from heart, as the total volume of the circulatory container should
    /// become smaller (or bigger if a body part is added).
    /// </summary>
    private void UpdateVolume()
    {
        List<BodyPart> connectedToHeart = GetAllBodyPartAttachedToHeart();
        _container.ChangeVolume((float)connectedToHeart.Sum(x => x.Volume) * HealthConstants.BloodVolumeToHumanVolumeRatio);
    }

    public void HandleHeartPulse(object sender, EventArgs args)
    {
        List<BodyPart> connectedToHeart = GetAllBodyPartAttachedToHeart();
        UpdateVolume();
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

    /// <summary>
    /// Send oxygen to all connected circulatory layers to heart.
    /// Send a bit more than necessary when oxygen is available to restore oxygen reserves in each circulatory layers.
    /// </summary>
    /// <param name="connectedToHeart"></param>
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
            double proportionAvailable = 0;
            if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
            {
                proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
                veins.ReceiveOxygen(HealthConstants.SafeOxygenFactor * proportionAvailable * availableOxygen);
            }
            else
            {
                proportionAvailable = oxygenNeededForEachpart[i] / sumNeeded;
                veins.ReceiveOxygen(proportionAvailable * availableOxygen);
            }
            i++;
        }

        if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
        {
            _container.RemoveSubstance(oxygen, HealthConstants.SafeOxygenFactor * sumNeeded);
        }
        else
        {
            _container.RemoveSubstance(oxygen, (float)availableOxygen);
        }

        SetBreathingState((float) availableOxygen, sumNeeded);
    }

    private void SetBreathingState(float availableOxygen, float sumNeeded)
    {
        if (availableOxygen > HealthConstants.SafeOxygenFactor * sumNeeded)
        {
            breathing = BreathingState.Nice; 
        }
        else if(availableOxygen > sumNeeded)
        {
            breathing = BreathingState.Difficult;
        }
        else
        {
            breathing = BreathingState.Suffocating;
        }

        Debug.Log(breathing.ToString());
    }

    /// <summary>
    /// Return the amount of oxygen the circulatory system can send to organs.
    /// If the blood quantity is above a given treshold, all oxygen in the circulatory container is available.
    /// If blood gets below, it starts diminishing the availability of oxygen despite the circulatory system containing enough.
    /// This is to mimick the lack of blood making oxygen transport difficult and potentially leading to organ suffocation.
    /// </summary>
    private double AvailableOxygen()
    {
        SubstancesSystem registry = Subsystems.Get<SubstancesSystem>();
        Substance blood = registry.FromType(SubstanceType.Blood);

        Substance oxygen = registry.FromType(SubstanceType.Oxygen);

        float bloodVolume = _container.GetSubstanceVolume(blood);

        float healthyBloodVolume = (float) HealthConstants.HealthyBloodVolumeRatio * _container.Volume;

        double oxygenQuantity = _container.GetSubstanceQuantity(oxygen);

        return bloodVolume > healthyBloodVolume ? oxygenQuantity : (bloodVolume / healthyBloodVolume) * oxygenQuantity;
    }

    /// <summary>
    /// Get a list of all body part attached to heart, including all internal organs.
    /// A body part is considered attached to heart if it's either the external body part of heart, 
    /// a child of the latter or an internal body part of any, with the condition that they need to have a circulatory layer.
    /// Fixing a living foot on a wooden leg won't prevent it from dying.
    /// </summary>
    private List<BodyPart> GetAllBodyPartAttachedToHeart()
    {
        List<BodyPart> connectedToHeart = new List<BodyPart>();

        if (_heart.IsInsideBodyPart)
        {
            BodyPart heartContainer = _heart.ExternalBodyPart;
            GetAllBodyPartAttachedToHeartRecursion(connectedToHeart, heartContainer);
        }
        return connectedToHeart;
    }

    /// <summary>
    /// Helper method for GetAllBodyPartAttachedToHeart().
    /// </summary>
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

    /// <summary>
    /// Compute the need in oxygen of every body part attached to heart.
    /// </summary>
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
