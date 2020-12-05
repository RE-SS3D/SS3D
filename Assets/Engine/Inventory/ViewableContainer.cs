using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// A container that can be accessed by a mob
    /// </summary>
    public class ViewableContainer : MonoBehaviour
    {
        /// <summary>
        /// A list of creatures accessing this container
        /// </summary>
        [NonSerialized] public readonly List<Accessor> Accessors = new List<Accessor>();

        public AttachedContainer AttachedContainer;

        public delegate void AccessHandler(ViewableContainer container, Accessor accessor);

        public event AccessHandler Accessed;

        public void Start()
        {
            
            if (NetworkClient.active && !NetworkServer.active)
            {
                Destroy(this);
                return;
            }
            
            if (AttachedContainer == null)
            {
                AttachedContainer = GetComponent<AttachedContainer>();
                Assert.IsNotNull(AttachedContainer);
            }
        }

        public void Access(Creature creature)
        {
            bool present = false;
            Accessor accessor = Accessors.FirstOrDefault(x => x.Creature == creature);
            if (accessor != null)
            {
                if (accessor.IsAccessing)
                {
                    return;
                }

                present = true;
            }
            else
            {
                accessor = new Accessor
                {
                    Creature = creature
                };
            }
            
            OnAccessed(accessor);

            accessor.IsAccessing = true;
            accessor.LastAccess = Time.time;

            if (!present)
            {
                Accessors.Add(accessor);
            }
        }

        /// <summary>
        /// If a creature is accessing this container
        /// </summary>
        /// <param name="creature">The creature to check</param>
        public bool IsAccessing(Creature creature)
        {
            foreach (Accessor accessor in Accessors)
            {
                if (accessor.IsAccessing && accessor.Creature == creature)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If a creature should be able to look into the container
        /// </summary>
        public virtual bool CanAccess(Creature creature)
        {
            return true;
        }
        
        /// <summary>
        /// If a creature should be able to modify the contents of this container
        /// </summary>
        public virtual bool CanModify(Creature creature)
        {
            return true;
        }

        protected virtual void OnAccessed(Accessor accessor)
        {
            AttachedContainer.AddObserver(accessor.Creature);
            Accessed?.Invoke(this, accessor);
        }

        public class Accessor
        {
            public Creature Creature { get; set; }
            public float LastAccess { get; set; }
            public bool IsAccessing { get; set; }
        }
    }
}