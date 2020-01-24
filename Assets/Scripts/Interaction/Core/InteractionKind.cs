using UnityEngine;

namespace Interaction.Core
{
    [CreateAssetMenu(fileName = "Interaction", menuName = "Interaction/Interaction Kind", order = 0)]
    public class InteractionKind : ScriptableObject
    {
        [SerializeField] private string description = "";
        public string Description => description;
    }
}