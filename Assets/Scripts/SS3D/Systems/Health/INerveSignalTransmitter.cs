using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Interface to allow for nerve signals to be transmitted not only by nerve body layers,
/// but also by other stuff, machines or alien stuff.
/// </summary>
public interface INerveSignalTransmitter
{
    /// <summary>
    /// All direcly connected parent nerve signal transmitters
    /// (e.g. if this is neck, head is directly connected as parent and transmitting nerve signal for humans).
    /// </summary>
    public List<INerveSignalTransmitter> ParentConnectedSignalTransmitters();

    /// <summary>
    /// All direcly connected parent nerve signal transmitters
    /// (e.g. if this is head, neck is directly connected as child and transmitting nerve signal for humans).
    /// </summary>
    public List<INerveSignalTransmitter> ChildConnectedSignalTransmitters();

    public NerveSignalTransmitterEnum TransmitterId { get; set; }

    /// <summary>
    /// Is there a continous chain of nerve signal transmitter between this and the central nervous system ?
    /// True if there is and none are destroyed, false otherwise.
    /// </summary>
    public bool IsConnectedToCentralNervousSystem { get; set; }

    /// <summary>
    /// Remove a transmitter from the connected signal transmitters collection.
    /// </summary>
    public void RemoveNerveSignalTransmitter(INerveSignalTransmitter transmitter);

    /// <summary>
    /// Add a transmitter to the connected signal transmitters collection, as parent or as child.
    /// </summary>
    public void AddNerveSignalTransmitter(INerveSignalTransmitter transmitter, bool isChild);

    /// <summary>
    /// Check if transmitter is already directly connected to this.
    /// </summary>
    public bool AlreadyAdded(INerveSignalTransmitter transmitter);

    /// <summary>
    /// Disconnect this from all nerve signal transmitter.
    /// </summary>
    public void RemoveAllNerveSignalTransmitter();

    public float ProducePain();

    /// <summary>
    /// Disconnect this from the central nervous system. 
    /// When disconnected, as other things, no pain is produced anymore.
    /// </summary>
    public void DisconnectFromCentralNervousSystem();



}
