using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Necessary as we expect body 
/// </summary>
public interface INerveSignalTransmitter
{
    public List<INerveSignalTransmitter> ParentConnectedSignalTransmitters();
    public List<INerveSignalTransmitter> ChildConnectedSignalTransmitters();

    public bool IsConnectedToCentralNervousSystem { get; set; }

    /// <summary>
    /// Add 
    /// </summary>
    /// <param name="transmitter"></param>
    public void RemoveNerveSignalTransmitter(INerveSignalTransmitter transmitter);


    public void AddNerveSignalTransmitter(INerveSignalTransmitter transmitter, bool isChild);

    public void RemoveAllNerveSignalTransmitter();

    public float ProducePain();

    public void DisconnectFromCentralNervousSystem();

}
