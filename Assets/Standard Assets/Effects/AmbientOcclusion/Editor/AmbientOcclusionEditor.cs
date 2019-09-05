using UnityEngine;
using UnityEditor;

namespace UnityStandardAssets.CinematicEffects
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AmbientOcclusion))]
    public class AmbientOcclusionEditor : Editor
    {
        SerializedProperty _intensity;
        SerializedProperty _radius;
        SerializedProperty _sampleCount;
        SerializedProperty _sampleCountValue;
        SerializedProperty _downsampling;
        SerializedProperty _occlusionSource;
        SerializedProperty _ambientOnly;
        SerializedProperty _debug;

        static GUIContent _textValue = new GUIContent("Value");

        static string _textNoGBuffer =
            "G-buffer is currently unavailable. " +
            "Change Renderring Path in camera settings to Deferred.";

        static string _textNoAmbientOnly =
            "Ambient-only mode is currently disabled; " +
            "it requires G-buffer source and HDR rendering.";

        static string _textGBufferNote =
            "Forward opaque objects don't go in the G-buffer. " +
            "This may lead to artifacts.";

        #if UNITY_5_4_OR_NEWER
        static string _textSinglePassStereo =
            "Ambient-only mode isn't supported in single-pass stereo rendering.";
        #endif

        void OnEnable()
        {
            _intensity = serializedObject.FindProperty("settings.intensity");
            _radius = serializedObject.FindProperty("settings.radius");
            _sampleCount = serializedObject.FindProperty("settings.sampleCount");
            _sampleCountValue = serializedObject.FindProperty("settings.sampleCountValue");
            _downsampling = serializedObject.FindProperty("settings.downsampling");
            _occlusionSource = serializedObject.FindProperty("settings.occlusionSource");
            _ambientOnly = serializedObject.FindProperty("settings.ambientOnly");
            _debug = serializedObject.FindProperty("settings.debug");
        }

        public override void OnInspectorGUI()
        {
            var targetInstance = (AmbientOcclusion)target;

            serializedObject.Update();

            EditorGUILayout.PropertyField(_intensity);
            EditorGUILayout.PropertyField(_radius);
            EditorGUILayout.PropertyField(_sampleCount);

            if (_sampleCount.hasMultipleDifferentValues ||
                _sampleCount.enumValueIndex == (int)AmbientOcclusion.SampleCount.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_sampleCountValue, _textValue);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_downsampling);
            EditorGUILayout.PropertyField(_occlusionSource);

            if (!_occlusionSource.hasMultipleDifferentValues &&
                _occlusionSource.enumValueIndex == (int)AmbientOcclusion.OcclusionSource.GBuffer)
            {
                if (!targetInstance.isGBufferAvailable)
                    EditorGUILayout.HelpBox(_textNoGBuffer, MessageType.Warning);
                else if (!_ambientOnly.hasMultipleDifferentValues && !_ambientOnly.boolValue)
                    EditorGUILayout.HelpBox(_textGBufferNote, MessageType.Info);
            }

            EditorGUILayout.PropertyField(_ambientOnly);

            if (!_ambientOnly.hasMultipleDifferentValues &&
                _ambientOnly.boolValue &&
                !targetInstance.isAmbientOnlySupported)
            {
                EditorGUILayout.HelpBox(_textNoAmbientOnly, MessageType.Warning);
            }

            #if UNITY_5_5_OR_NEWER
            if (_ambientOnly.boolValue && PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass || PlayerSettings.stereoRenderingPath == StereoRenderingPath.Instancing)
                EditorGUILayout.HelpBox(_textSinglePassStereo, MessageType.Warning);
            #elif UNITY_5_4_OR_NEWER
            if (_ambientOnly.boolValue && PlayerSettings.singlePassStereoRendering)
                EditorGUILayout.HelpBox(_textSinglePassStereo, MessageType.Warning);
            #endif

            EditorGUILayout.PropertyField(_debug);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
