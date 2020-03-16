using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SS3D.Engine.Tiles.State
{
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

        public object GetTileState()
        {
            return TileState;
        }
        public void SetTileState(object obj)
        {
            var prevState = tileState;

            tileState = (T) obj;
            OnStateUpdate(prevState);
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
}