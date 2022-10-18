using System.Collections.Generic;
using System.Linq;
using Coimbra;
using UnityEngine;

namespace SS3D.Interactions.UI.RadialMenuInteraction
{
    public class PetalFolder
    {
        public GameObject PetalPrefab;
        public readonly List<Petal> Petals;
        public PetalFolder Folder;
        public bool IsDirty;

        public PetalFolder(GameObject prefab)
        {
            Petals = new List<Petal>();
            Folder = null;
            IsDirty = true;
            PetalPrefab = prefab;
        }

        public bool AddPetal(Petal petal)
        {
            Petals.Add(petal);
            IsDirty = true;
            return true;
        }

        public bool CheckAnimationDone()
        {
            return Petals.All(petal => petal.IsAnimationInProgress());
        }

        public void Clear()
        {
            foreach (Petal petal in Petals)
            {
                petal.Destroy();
            }
        }

        public void Disable()
        {
            foreach (Petal petal in Petals)
            {
                petal.gameObject.SetActive(false);
            }
        }

        public void Enable()
        {
            foreach (Petal petal in Petals)
            {
                petal.gameObject.SetActive(true);
            }
        }
    }
}
