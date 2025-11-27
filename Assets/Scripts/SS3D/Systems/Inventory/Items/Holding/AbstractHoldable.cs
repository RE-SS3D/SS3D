using NaughtyAttributes;
using SS3D.Core.Behaviours;
using SS3D.Intents;
using SS3D.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public abstract class AbstractHoldable : NetworkActor
    {
        [SerializeField]
        private Transform _primaryRightHandHold;

        [SerializeField]
        private Transform _primaryLeftHandHold;

        [ShowIf(nameof(CanHoldTwoHand))]
        [SerializeField]
        private Transform _secondaryRightHandHold;

        [ShowIf(nameof(CanHoldTwoHand))]
        [SerializeField]
        private Transform _secondaryLeftHandHold;

        public abstract bool CanHoldTwoHand { get; }

        public abstract FingerPoseType PrimaryHandPoseType { get; }

        public abstract FingerPoseType SecondaryHandPoseType { get; }

        public abstract HandHoldType SingleHandHold { get; }

        public abstract HandHoldType TwoHandHold { get; }

        public abstract HandHoldType SingleHandHoldHarm { get; }

        public abstract HandHoldType TwoHandHoldHarm { get; }

        public HandHoldType GetHoldType(bool withTwoHands, IntentType intent)
        {
            switch (intent, withTwoHands)
            {
                case (IntentType.Help, true):
                    return TwoHandHold;
                case (IntentType.Help, false):
                    return SingleHandHold;
                case (IntentType.Harm, true):
                    return TwoHandHoldHarm;
                case (IntentType.Harm, false):
                    return SingleHandHoldHarm;
            }

            return SingleHandHold;
        }

        public Transform GetHold(bool primary, HandType handType)
        {
            switch (primary, handType)
            {
                case (true, HandType.LeftHand):
                    return _primaryLeftHandHold != null ? _primaryLeftHandHold : transform;
                case (false, HandType.LeftHand):
                    return _secondaryLeftHandHold != null ? _secondaryLeftHandHold : transform;
                case (true, HandType.RightHand):
                    return _primaryRightHandHold != null ? _primaryRightHandHold : transform;
                case (false, HandType.RightHand):
                    return _secondaryRightHandHold != null ? _secondaryRightHandHold : transform;
                default:
                    throw new ArgumentException();
            }
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            // Make sure gizmo only draws in prefab mode
            if (EditorApplication.isPlaying || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                return;
            }

            DrawHands();
        }

        private void DrawHands()
        {
            Mesh leftHandGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Entities/Humanoids/Human/HumanHandLeft.mesh", typeof(Mesh));
            Mesh rightHandGuide = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Art/Models/Entities/Humanoids/Human/HumanHandRight.mesh", typeof(Mesh));

            if (_primaryRightHandHold != null && UnityEditor.Selection.activeTransform == _primaryRightHandHold)
            {
                DrawHand(rightHandGuide, true, _primaryRightHandHold.position, _primaryRightHandHold.localRotation, new Color32(20, 120, 255, 200));
            }
            else if (_primaryLeftHandHold != null && UnityEditor.Selection.activeTransform == _primaryLeftHandHold)
            {
                DrawHand(leftHandGuide, false, _primaryLeftHandHold.position, _primaryLeftHandHold.localRotation, new Color32(255, 120, 20, 200));
            }
            else if (_secondaryRightHandHold != null && UnityEditor.Selection.activeTransform == _secondaryRightHandHold)
            {
                DrawHand(rightHandGuide, true, _secondaryRightHandHold.position, _secondaryRightHandHold.localRotation, new Color32(255, 120, 20, 200));
            }
            else if (_secondaryLeftHandHold != null && UnityEditor.Selection.activeTransform == _secondaryLeftHandHold)
            {
                DrawHand(leftHandGuide, false, _secondaryLeftHandHold.position, _secondaryLeftHandHold.localRotation, new Color32(20, 120, 255, 200));
            }
        }

        private void DrawHand(Mesh model, bool isRight, Vector3 position, Quaternion rotation, Color color)
        {
            Gizmos.color = color;

            if (isRight)
            {
                rotation *= Quaternion.AngleAxis(-90, Vector3.back);
                position += rotation * new Vector3(0.0875f, -0.0304f, -0.0184f);
            }
            else
            {
                rotation *= Quaternion.AngleAxis(90, Vector3.back);
                position += rotation * new Vector3(-0.0875f, -0.0304f, -0.0184f);
            }

            Gizmos.DrawMesh(model, position, rotation);
        }
#endif

    }
}
