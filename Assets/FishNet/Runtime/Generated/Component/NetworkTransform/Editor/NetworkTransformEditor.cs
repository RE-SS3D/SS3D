﻿#if UNITY_EDITOR
using FishNet.Editing;
using UnityEditor;
using UnityEngine;

namespace FishNet.Component.Transforming.Editing
{


    [CustomEditor(typeof(NetworkTransform), true)]
    [CanEditMultipleObjects]
    public class NetworkTransformEditor : Editor
    {
        private SerializedProperty _componentConfiguration;
        private SerializedProperty _synchronizeParent;
        private SerializedProperty _packing;
        private SerializedProperty _interpolation;
        private SerializedProperty _extrapolation;
        private SerializedProperty _enableTeleport;
        private SerializedProperty _teleportThreshold;
        private SerializedProperty _clientAuthoritative;
        private SerializedProperty _sendToOwner;
        private SerializedProperty _useNetworkLod;
        private SerializedProperty _interval;
        private SerializedProperty _synchronizePosition;
        private SerializedProperty _positionSnapping;
        private SerializedProperty _synchronizeRotation;
        private SerializedProperty _rotationSnapping;
        private SerializedProperty _synchronizeScale;
        private SerializedProperty _scaleSnapping;


        protected virtual void OnEnable()
        {
            _componentConfiguration = serializedObject.FindProperty(nameof(_componentConfiguration));
            _synchronizeParent = serializedObject.FindProperty("_synchronizeParent");
            _packing = serializedObject.FindProperty("_packing");
            _interpolation = serializedObject.FindProperty("_interpolation");
            _extrapolation = serializedObject.FindProperty("_extrapolation");
            _enableTeleport = serializedObject.FindProperty("_enableTeleport");
            _teleportThreshold = serializedObject.FindProperty("_teleportThreshold");
            _clientAuthoritative = serializedObject.FindProperty("_clientAuthoritative");
            _sendToOwner = serializedObject.FindProperty("_sendToOwner");
            _useNetworkLod = serializedObject.FindProperty(nameof(_useNetworkLod));
            _interval = serializedObject.FindProperty(nameof(_interval));
            _synchronizePosition = serializedObject.FindProperty("_synchronizePosition");
            _positionSnapping = serializedObject.FindProperty("_positionSnapping");
            _synchronizeRotation = serializedObject.FindProperty("_synchronizeRotation");
            _rotationSnapping = serializedObject.FindProperty("_rotationSnapping");
            _synchronizeScale = serializedObject.FindProperty("_synchronizeScale");
            _scaleSnapping = serializedObject.FindProperty("_scaleSnapping");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((NetworkTransform)target), typeof(NetworkTransform), false);
            GUI.enabled = true;

            
#pragma warning disable CS0162 // Unreachable code detected
                EditorGUILayout.HelpBox(EditingConstants.PRO_ASSETS_LOCKED_TEXT, MessageType.Warning);
#pragma warning restore CS0162 // Unreachable code detected

            //Misc.
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_componentConfiguration);
            EditorGUILayout.PropertyField(_synchronizeParent, new GUIContent("* Synchronize Parent"));
            EditorGUILayout.PropertyField(_packing);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            //Smoothing.
            EditorGUILayout.LabelField("Smoothing", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_interpolation);
            EditorGUILayout.PropertyField(_extrapolation, new GUIContent("* Extrapolation"));
            EditorGUILayout.PropertyField(_enableTeleport);
            if (_enableTeleport.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_teleportThreshold);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            //Authority.
            EditorGUILayout.LabelField("Authority", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_clientAuthoritative);
            if (!_clientAuthoritative.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_sendToOwner);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            //Synchronizing.
            EditorGUILayout.LabelField("Synchronizing.", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            //LOD and interval.
            EditorGUILayout.PropertyField(_useNetworkLod, new GUIContent("Use Network Level of Detail"));
            if (!_useNetworkLod.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_interval, new GUIContent("Send Interval"));
                EditorGUI.indentLevel--;
            }
            //Position.
            EditorGUILayout.PropertyField(_synchronizePosition);
            if (_synchronizePosition.boolValue)
            {
                EditorGUI.indentLevel += 2;
                EditorGUILayout.PropertyField(_positionSnapping);
                EditorGUI.indentLevel -= 2;
            }
            //Rotation.
            EditorGUILayout.PropertyField(_synchronizeRotation);
            if (_synchronizeRotation.boolValue)
            {
                EditorGUI.indentLevel += 2;
                EditorGUILayout.PropertyField(_rotationSnapping);
                EditorGUI.indentLevel -= 2;
            }
            //Scale.
            EditorGUILayout.PropertyField(_synchronizeScale);
            if (_synchronizeScale.boolValue)
            {
                EditorGUI.indentLevel += 2;
                EditorGUILayout.PropertyField(_scaleSnapping);
                EditorGUI.indentLevel -= 2;
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif