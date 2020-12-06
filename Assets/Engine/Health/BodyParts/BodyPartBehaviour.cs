using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Health
{

    public class BodyPartBehaviour : MonoBehaviour
    {
        private float bruteDamage;
        private float burnDamage;
        public float BruteDamage { get { return bruteDamage; } set { bruteDamage = Mathf.Clamp(value, 0, MaxDamage); } }
        public float BurnDamage { get { return burnDamage; } set { burnDamage = Mathf.Clamp(value, 0, MaxDamage); } }

        public int MaxDamage = 200;
        public BodyPartType Type;

        /// Specifies prefab to spawn if this body part is detached
        [SerializeField] private GameObject severedBodyPartPrefab = null;

        /// The skinnedMeshRenderer associated with this bodypart. It will be hidden if the bodypart is detached
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer = null;

        /// List of children for this bodypart. For example, the hand should be a child of the arm, etc.
        [SerializeField] private List<BodyPartBehaviour> childrenParts = new List<BodyPartBehaviour>();

        public bool isBleeding = false;
        public CreatureHealth creatureHealth;

        public DamageSeverity Severity;
        public float OverallDamage => BruteDamage + BurnDamage;
        public List<BodyPartBehaviour> ChildrenParts => childrenParts;
        public GameObject SeveredBodyPartPrefab => severedBodyPartPrefab;
        public SkinnedMeshRenderer SkinnedMeshRenderer => skinnedMeshRenderer;

        private bool isSevered = false;

        /// Randomized hit zone. 0f for totally random, 0.99f for 99% chance of provided one
        /// <param name="aim"></param>
        /// <param name="hitProbability">0f to 1f: chance of hitting the requested body part</param>
        public static BodyPartType Randomize(BodyPartType aim, float hitProbability = 0.8f)
        {
            float normalizedRange = Mathf.Clamp(hitProbability, 0f, 1f);
            if (Random.value < (normalizedRange / 100f))
            {
                return aim;
            }

            // TODO: Add all limbs and put some realistic chances in
            int t = (int)Mathf.Floor(Random.value * 50);
            //	3/50
            if (t <= 3)
                return BodyPartType.Head;
            if (t <= 10)
                //	7/50
                return BodyPartType.ArmLeft;
            if (t <= 17)
                //	7/50
                return BodyPartType.ArmRight;
            if (t <= 24)
                //	7/50
                return BodyPartType.LegLeft;
            if (t <= 31)
                //	7/50
                return BodyPartType.LegRight;
            if (t <= 41)
                //	7/50
                return BodyPartType.Chest;
            //	9/50
            return BodyPartType.Chest;
        }

        // Apply damages from here.
        public virtual void ReceiveDamage(DamageType damageType, float damage)
        {
            UpdateDamage(damage, damageType);
        }

        private void UpdateDamage(float damage, DamageType type)
        {
            switch (type)
            {
                case DamageType.Brute:
                    BruteDamage += damage;
                    break;

                case DamageType.Burn:
                    BurnDamage += damage;
                    break;
            }
            UpdateSeverity();
        }

        // Restore damage from here
        public virtual void HealDamage(int damage, DamageType type)
        {
            switch (type)
            {
                case DamageType.Brute:
                    BruteDamage -= damage;
                    if (BruteDamage < 20)
                    {
                        creatureHealth.bloodSystem.StopBleeding(this);
                    }
                    break;

                case DamageType.Burn:
                    BurnDamage -= damage;
                    break;
            }
            UpdateSeverity();
        }

        public float GetDamageValue(DamageType damageType)
        {
            if (damageType == DamageType.Brute)
            {
                return BruteDamage;
            }
            if (damageType == DamageType.Burn)
            {
                return BurnDamage;
            }
            return 0;
        }

        private void UpdateSeverity()
        {
            // update UI limbs depending on their severity of damage
            float severity = (float)OverallDamage / MaxDamage;

            // If the limb is uninjured
            if (severity <= 0)
            {
                Severity = DamageSeverity.None;
            }
            // If the limb is under 20% damage
            else if (severity < 0.2)
            {
                Severity = DamageSeverity.Light;
            }
            // If the limb is under 40% damage
            else if (severity < 0.4)
            {
                Severity = DamageSeverity.LightModerate;
            }
            // If the limb is under 60% damage
            else if (severity < 0.6)
            {
                Severity = DamageSeverity.Moderate;
            }
            // If the limb is under 80% damage
            else if (severity < 0.8)
            {
                Severity = DamageSeverity.Bad;
            }
            // If the limb is under 100% damage
            else if (severity < 1f)
            {
                Severity = DamageSeverity.Critical;
            }
            // If the limb is 100% damage or over
            else if (severity >= 1f)
            {
                Severity = DamageSeverity.Max;
                SeverBodyPart();
            }

            Debug.Log(("Limb " + Type.ToString() + "  has " + Severity.ToString()) + " damage");

            UpdateUi();
        }

        private List<BodyPartBehaviour> GetAllBodyParts(BodyPartBehaviour root)
        {
            List<BodyPartBehaviour> bodyParts = new List<BodyPartBehaviour>();

            foreach (BodyPartBehaviour child in root.childrenParts)
            {
                if (child != null)
                {
                    bodyParts.Add(child);
                    bodyParts.AddRange(child.childrenParts);
                }
            }

            return bodyParts;
        }

        private void SeverBodyPart()
        {
            if (severedBodyPartPrefab == null)
            {
                Debug.LogError($"No SeveredBodyPart defined on the BodyParts {gameObject.name}");
            }

            if (!isSevered)
            {
                // Spawn the severed body part
                GameObject mainBodypart = Instantiate(severedBodyPartPrefab, transform.position, Quaternion.identity);

                // Update the skinned mesh renderer
                UpdateBodyPartVisuals(mainBodypart.GetComponent<SkinnedMeshRenderer>(), skinnedMeshRenderer);

                // Spawn the severed part for other clients as well
                // NetworkServer.Spawn(mainBodypart);


                // Do childeren body parts as well
                GetAllBodyParts(this).ForEach(child =>
                {
                    GameObject childBodyPart = Instantiate(child.SeveredBodyPartPrefab, child.transform.position, Quaternion.identity);
                    UpdateBodyPartVisuals(childBodyPart.GetComponent<SkinnedMeshRenderer>(), child.SkinnedMeshRenderer);

                    /// Freeze the rigid body to the child parts remain attached
                    //childBodyPart.transform.SetParent(mainBodypart.transform);
                    //Rigidbody childRigid = childBodyPart.GetComponent<Rigidbody>();
                    //childRigid.constraints = RigidbodyConstraints.FreezeAll;

                    child.HideSeveredBodyPart();

                    // NetworkServer.Spawn(childBodyPart);
                });

                HideSeveredBodyPart();
                isSevered = true;
            }
        }

        private void HideSeveredBodyPart()
        {
            skinnedMeshRenderer.enabled = false;
        }

        private void UpdateBodyPartVisuals(SkinnedMeshRenderer newBodyPart, SkinnedMeshRenderer bodyPart)
        {
            Material[] materials = bodyPart.sharedMaterials;
            newBodyPart.materials = materials;

            for (int i = 0; i < newBodyPart.sharedMesh.blendShapeCount; i++)
            {
                newBodyPart.SetBlendShapeWeight(i, bodyPart.GetBlendShapeWeight(i));
            }
        }


        // TODO: updates the health UI 
        private void UpdateUi()
        {

        }

        public virtual void RestoreDamage()
        {
            BruteDamage = 0;
            BurnDamage = 0;
            UpdateSeverity();
        }

        // --------------------
        // UPDATES FROM SERVER
        // --------------------
        public void UpdateClientBodyPartStat(float _bruteDamage, float _burnDamage)
        {
            BruteDamage = _bruteDamage;
            BurnDamage = _burnDamage;
            UpdateSeverity();
        }
    }
}