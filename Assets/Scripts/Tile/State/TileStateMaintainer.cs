using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/**
 * A component used by any tilemap tile to store data.
 * Note: We don't just serialize the whole component, because the component may be serializing
 * unnecessary parameters to show in unity.
 * 
 * T - The struct/class storing the data.
 */
public class TileStateMaintainer<T> : MonoBehaviour, TileStateCommunicator
{
    // The main reason its public and not protected is so that the editor can access it.
    public T TileState => tileState;

    public byte[] GetTileState()
    {
        using (MemoryStream stream = new MemoryStream()) {
            new BinaryFormatter().Serialize(stream, TileState);
            return stream.ToArray();
        }
    }

    public void UpdateTileState(byte[] data)
    {
        var prevData = TileState;

        using (MemoryStream stream = new MemoryStream(data)) {
            tileState = (T)new BinaryFormatter().Deserialize(stream);
        }

        // TODO: If we had dots this action should really be queued.
        OnStateUpdate(prevData);
    }

    /**
     * Update aspects of the tile based on the new tile state.
     * Note: this.TileState will be updated with new data. If needed, the previous state is passed as a parameter.
     */
    protected virtual void OnStateUpdate(T prevData)
    {
    }

    // We can't use a property with private set, due to the SerializeField requirement
    [SerializeField]
    private T tileState;
}
