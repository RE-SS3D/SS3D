using Codice.Client.Commands;
using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class HumanBodypart : BodyPart, INerveSignalTransmitter
{
    protected MuscleLayer muscleLayer;
    protected BoneLayer boneLayer;
    protected CirculatoryLayer circulatoryLayer;
    protected NerveLayer nerveLayer;

    [SerializeField]
    protected List<INerveSignalTransmitter> ConnectedParentNerveSignalTransmitters;

    [SerializeField]
    protected List<INerveSignalTransmitter> ConnectedChildNerveSignalTransmitters;

    public bool IsConnectedToCentralNervousSystem { get; set; }

    public HumanBodypart()
    {
        muscleLayer = new MuscleLayer(this);
        boneLayer = new BoneLayer(this);
        circulatoryLayer= new CirculatoryLayer(this);
        nerveLayer= new NerveLayer(this);

        BodyLayers.Add(muscleLayer);
        BodyLayers.Add(boneLayer);
        BodyLayers.Add(circulatoryLayer);
        BodyLayers.Add(nerveLayer);

        nerveLayer.DamageReceivedEvent += OnNerveDamaged;
    }

    private void OnNerveDamaged(object sender, DamageEventArgs nerveDamageEventArgs)
    {
        if(sender is not NerveLayer)
        {
            return;
        }

        var nervelayer = (NerveLayer)sender;
        if (nervelayer == null || nervelayer.IsDestroyed())
        {
            RemoveAllNerveSignalTransmitter();
        }
    }

    /// <summary>
    /// Disconnect all child nerve signal transmitters as well as this from the CNS.
    /// </summary>
    public void DisconnectFromCentralNervousSystem()
    {
        foreach (INerveSignalTransmitter transmitter in ConnectedChildNerveSignalTransmitters)
        {
            transmitter.DisconnectFromCentralNervousSystem();
        }
        IsConnectedToCentralNervousSystem = false;
    }

    public void RemoveAllNerveSignalTransmitter()
    {
        DisconnectFromCentralNervousSystem();
        foreach (INerveSignalTransmitter transmitter in ConnectedChildNerveSignalTransmitters)
        {
            transmitter.RemoveNerveSignalTransmitter(this);
        }
        foreach (INerveSignalTransmitter transmitter in ConnectedParentNerveSignalTransmitters)
        {
            transmitter.RemoveNerveSignalTransmitter(this);
        }
        ConnectedParentNerveSignalTransmitters.Clear();
        ConnectedChildNerveSignalTransmitters.Clear();
    }

    public List<INerveSignalTransmitter> ParentConnectedSignalTransmitters()
    {
        return ConnectedParentNerveSignalTransmitters;
    }

    public List<INerveSignalTransmitter> ChildConnectedSignalTransmitters()
    {
        return ConnectedParentNerveSignalTransmitters;
    }

    public void RemoveNerveSignalTransmitter(INerveSignalTransmitter transmitter)
    {
        if(transmitter == null) { return; }
        ConnectedChildNerveSignalTransmitters.Remove(transmitter);
        ConnectedParentNerveSignalTransmitters.Remove(transmitter);
        transmitter.RemoveNerveSignalTransmitter(this);
    }

    public void AddNerveSignalTransmitter(INerveSignalTransmitter transmitter, bool isChild)
    {
        if (transmitter == null) { return; }
        if (isChild)
        {
            ConnectedChildNerveSignalTransmitters.Add(transmitter);
        }
        else
        {
            ConnectedParentNerveSignalTransmitters.Add(transmitter);
        }

        transmitter.AddNerveSignalTransmitter(this, !isChild);
    }

    public float ProducePain()
    {
        if (nerveLayer == null || nerveLayer.IsDestroyed()) { return 0.0f; }
        return nerveLayer.ProducePain();
    }
}
