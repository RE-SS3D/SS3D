using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// References to key humanoid bones for item attachment, IK, and avatar setup.
    /// Resolves rig prerequisites for milestone 0.0.8 until a formal Avatar asset is imported.
    /// </summary>
    public class HumanoidRigReferences : MonoBehaviour
    {
        [Header("Core Bones")]
        [SerializeField] private Transform _armatureRoot;
        [SerializeField] private Transform _hips;
        [SerializeField] private Transform _spine;
        [SerializeField] private Transform _chest;
        [SerializeField] private Transform _head;

        [Header("Arms")]
        [SerializeField] private Transform _handLeft;
        [SerializeField] private Transform _handRight;
        [SerializeField] private Transform _upperArmLeft;
        [SerializeField] private Transform _upperArmRight;

        [Header("Legs")]
        [SerializeField] private Transform _footLeft;
        [SerializeField] private Transform _footRight;

        public Transform ArmatureRoot => _armatureRoot;
        public Transform Hips => _hips;
        public Transform Spine => _spine;
        public Transform Chest => _chest;
        public Transform Head => _head;
        public Transform HandLeft => _handLeft;
        public Transform HandRight => _handRight;
        public Transform UpperArmLeft => _upperArmLeft;
        public Transform UpperArmRight => _upperArmRight;
        public Transform FootLeft => _footLeft;
        public Transform FootRight => _footRight;

        /// <summary>
        /// Attempts to auto-bind bones by Mixamo-compatible naming convention.
        /// </summary>
        public void AutoBindBones()
        {
            if (_armatureRoot == null)
            {
                Transform found = transform.Find("HumanArmature");
                if (found == null)
                {
                    found = transform.Find("Armature");
                }
                if (found == null)
                {
                    foreach (Transform child in transform)
                    {
                        if (child.name.Contains("Armature", System.StringComparison.OrdinalIgnoreCase))
                        {
                            found = child;
                            break;
                        }
                    }
                }
                _armatureRoot = found;
            }

            if (_hips == null)
            {
                _hips = FindBone("hips");
            }

            if (_spine == null)
            {
                _spine = FindBone("spine");
            }

            if (_chest == null)
            {
                _chest = FindBone("chest");
            }

            if (_head == null)
            {
                _head = FindBone("head");
            }

            if (_handLeft == null)
            {
                _handLeft = FindBone("hand_l");
            }

            if (_handRight == null)
            {
                _handRight = FindBone("hand_r");
            }

            if (_upperArmLeft == null)
            {
                _upperArmLeft = FindBone("upper_arm_l");
            }

            if (_upperArmRight == null)
            {
                _upperArmRight = FindBone("upper_arm_r");
            }

            if (_footLeft == null)
            {
                _footLeft = FindBone("foot_l");
            }

            if (_footRight == null)
            {
                _footRight = FindBone("foot_r");
            }
        }

        private Transform FindBone(string boneName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }
            return null;
        }

        public bool IsValid()
        {
            return _handLeft != null && _handRight != null && _hips != null;
        }
    }
}
