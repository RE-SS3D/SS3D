using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory
{
    public class VisibleContainer : MonoBehaviour
    {
        /// <summary>
        /// How often the observer list should be updated
        /// </summary>
        public float CheckObserversInterval = 1f;

        /// <summary>
        /// The container to watch
        /// </summary>
        public AttachedContainer AttachedContainer;

        private float lastObserverCheck;
        private List<Creature> currentObservers;

        public void Start()
        {
            if (NetworkClient.active && !NetworkServer.active)
            {
                Destroy(this);
                return;
            }

            currentObservers = new List<Creature>();
            if (AttachedContainer == null)
            {
                AttachedContainer = GetComponent<AttachedContainer>();
                Assert.IsNotNull(AttachedContainer);
            }
        }

        public void Update()
        {
            if (lastObserverCheck + CheckObserversInterval < Time.time)
            {
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                {
                    if (connection != null && connection.identity != null)
                    {
                        var creature = connection.identity.GetComponent<Creature>();
                        if (creature == null)
                        {
                            continue;
                        }
                        if (creature.CanSee(gameObject))
                        {
                            if (currentObservers.Contains(creature))
                            {
                                continue;
                            }

                            if (AttachedContainer.AddObserver(creature))
                            {
                                currentObservers.Add(creature);
                            }
                        }
                        else if (currentObservers.Remove(creature))
                        {
                            AttachedContainer.RemoveObserver(creature);
                        }
                    }
                }

                lastObserverCheck = Time.time;
            }
        }
    }
}