using System.Linq;
using UnityEngine;

namespace SS3D.Engine.Substances
{
    public class SubstanceRegistry : MonoBehaviour
    {
        public Substance[] Substances => substances;
        [SerializeField]
        private Substance[] substances;

        public Substance FromId(string id)
        {
            return Substances.FirstOrDefault(x => x.Id == id);
        }
    }
}
