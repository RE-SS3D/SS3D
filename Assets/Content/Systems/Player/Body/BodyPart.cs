using System.Collections.Generic;
using UnityEngine;
using SS3D.Engine.Health;

namespace SS3D.Content.Systems.Player.Body
{
    /// <summary>
    /// Class is responsible for handling state changes associated with a particular body part.
    /// MonoBehaviour should be attached to the game object that contains the hitbox collider of the particular body part.
    /// Each body part with its own collider should also have an instance of this component.
    /// </summary>
    public class BodyPart : MonoBehaviour
    {
        /// Specifies which body part this is
        [SerializeField] private BodyPartType bodyPartType = BodyPartType.Head;
        /// Specifies prefab to spawn if this body part is detached
        [SerializeField] private GameObject severedBodyPartPrefab = null;
        /// List of children for this bodypart. For example, the hand should be a child of the arm, etc.
        [SerializeField] private List<BodyPart> childrenParts = new List<BodyPart>();
        /// Flag enum, storing the current active statuses for this bodypart
        [SerializeField] private BodyPartStatuses bodyPartStatuses = BodyPartStatuses.Healthy;
        /// The skinnedMeshRenderer associated with this bodypart. It will be hidden if the bodypart is detached
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer = null;
        /// The body component that this bodypart belongs to
        [SerializeField] private Body body;

        private void Start()
        {
            if (severedBodyPartPrefab == null)
            {
                Debug.LogError($"No SeveredBodyPart defined on the Body {body.gameObject.name} on the BodyParts {gameObject.name}");
            }
            if (body == null)
            {
                body = transform.root.GetComponent<Body>();
            }
            if (severedBodyPartPrefab != null && severedBodyPartPrefab.GetComponent<SeveredBodyPart>() == null)
            {
                Debug.LogError($"SeveredBodyPartPrefab on BodyPart {gameObject.name} on Body {body.gameObject.name} is missing a SeveredBodyPart component!");
            }
        }

        public BodyPartType BodyPartType => bodyPartType;
        public GameObject SeveredBodyPartPrefab => severedBodyPartPrefab;
        public List<BodyPart> ChildrenParts => childrenParts;
        public BodyPartStatuses BodyPartStatuses => bodyPartStatuses;
        public List<BodyPartDamage> BodyPartDamages { get; } = new List<BodyPartDamage>
        {
            new BodyPartDamage(DamageType.Burn, 0f),
            new BodyPartDamage(DamageType.Brute, 0f),
            new BodyPartDamage(DamageType.Toxic, 0f),
            new BodyPartDamage(DamageType.Suffocation, 0f)
        };
        public SkinnedMeshRenderer SkinnedMeshRenderer => skinnedMeshRenderer;
        public Body Body => body;

        //Method responsible for determining if a new status should be added to the bodypart
        //TODO: currently, it only gets worse. Should implement logic to heal damages and remove a status from a bodypart
        public void EvaluateStatusChange(BodyPartStatuses status, float totalDamage, float serverAuthoritativeRandomRoll)
        {
            if (bodyPartStatuses.HasFlag(status))
            {
                return;
            }
            
            float requiredThreshold = DamageThresholds.OrganicMinimumRequiredDamage[status];
            if (requiredThreshold > totalDamage)
            {
                return;
            }

            float applyEffectRoll = serverAuthoritativeRandomRoll + totalDamage - requiredThreshold;
            Debug.Log($"{status} roll {applyEffectRoll}");
            if (applyEffectRoll < requiredThreshold)
            {
                return;
            }
            
            if (status == BodyPartStatuses.Severed && bodyPartType == BodyPartType.Chest)
            {
                return;
            }
            
            bodyPartStatuses |= status;
            PerformStatusAssociatedAction(status);
        }

        private void PerformStatusAssociatedAction(BodyPartStatuses status)
        {          
            switch (status)
            {
                case BodyPartStatuses.Numb:
                case BodyPartStatuses.Burned:
                case BodyPartStatuses.Bruised:
                case BodyPartStatuses.Crippled:
                case BodyPartStatuses.Bleeding:
                case BodyPartStatuses.Blistered:
                    return;
                case BodyPartStatuses.Severed:
                    body.SeverBodyPart(this);
                    break;
                default:
                    Debug.LogError($"BodyPart does not have damage handling for status {status}!");
                    break;
            }
        }
    }
}