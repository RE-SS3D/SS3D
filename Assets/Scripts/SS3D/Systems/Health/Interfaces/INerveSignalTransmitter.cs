using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

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
    public INerveSignalTransmitter ParentSignalTransmitter();

    /// <summary>
    /// All direcly connected parent nerve signal transmitters
    /// (e.g. if this is head, neck is directly connected as child and transmitting nerve signal for humans).
    /// </summary>
    public List<INerveSignalTransmitter> ChildSignalTransmitters();

    public NerveSignalTransmitterType TransmitterId { get; set; }

    /// <summary>
    /// The network object related to this nerve signal transmitter
    /// </summary>
    public NetworkObject getNetworkedObject { get; set; }

    public GameObject getGameObject { get; set; }

    public NetworkBehaviour GetNetworkBehaviour { get; set; }

    /// <summary>
    /// Is there a continous chain of nerve signal transmitter between this and the central nervous system ?
    /// True if there is and none are destroyed, false otherwise.
    /// </summary>
    public bool IsConnectedToCentralNervousSystem { get;}

    public float ProducePain();

    public bool CanTransmitSignal();

}
