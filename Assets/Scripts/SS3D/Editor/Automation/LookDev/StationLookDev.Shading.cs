using System;
using System.Linq;
using System.Reflection;
using System.Text;
using SS3D.Rendering.URP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Live <see cref="StationShading"/> edits for the look-dev round, callable from <c>unity command script eval</c>.
    /// </summary>
    public static partial class StationLookDev
    {
        /// <summary>The profile the player camera uses during a round; edits show on the next frame.</summary>
        public const string GameplayProfilePath = "Assets/Settings/URP/VolumeProfiles/SS3D_GameplayVolumeProfile.asset";

        /// <summary>URP's default profile: the baseline under every profile, and what a parameter reads with its override off.</summary>
        public const string DefaultProfilePath = "Assets/DefaultVolumeProfile.asset";

        /// <summary>
        /// Sets one Station Shading parameter (its field name, e.g. <c>LightStrength</c>) on the gameplay profile and
        /// turns its override on, recorded for Undo like an Inspector edit. It shows at once in Play Mode and stays
        /// after Play exits; it reaches disk with the next save. The parameters clamp to their slider range without a
        /// word, so the result says when the stored value differs from the one asked for.
        /// </summary>
        public static string SetShading(string parameter, float value)
        {
            StationShading shading = LoadShading(GameplayProfilePath);
            if (shading == null)
            {
                throw new InvalidOperationException($"{GameplayProfilePath} has no Station Shading override.");
            }

            ClampedFloatParameter target = ShadingParameter(shading, parameter);

            float before = target.value;
            Undo.RecordObject(shading, $"Look-dev {parameter}");
            target.overrideState = true;
            target.value = value;
            EditorUtility.SetDirty(shading);

            string result = $"{parameter}: {Format(before)} -> {Format(target.value)}";
            if (!Mathf.Approximately(target.value, value))
            {
                result += $" (clamped: {Format(value)} is outside [{Format(target.min)}, {Format(target.max)}])";
                Debug.LogWarning($"[look-dev] {result}");
            }
            else
            {
                Debug.Log($"[look-dev] {result}");
            }

            return result;
        }

        /// <summary>
        /// Writes the gameplay profile to disk, and nothing else. Prefer it to a project-wide save during a round:
        /// <see cref="StationLookDevSaveGuard"/> keeps the settings assets out of that, but everything else dirty goes.
        /// </summary>
        public static string SaveShading()
        {
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(GameplayProfilePath));
            return $"Saved {GameplayProfilePath}.";
        }

        /// <summary>
        /// One line per Station Shading parameter: the gameplay profile's value and override, the default profile's
        /// baseline, the value a round uses, and the slider range.
        /// </summary>
        public static string DescribeShading()
        {
            StationShading gameplay = LoadShading(GameplayProfilePath);
            StationShading baseline = LoadShading(DefaultProfilePath);
            StringBuilder text = new();
            text.AppendLine("parameter: used (gameplay override | baseline) [range]");

            foreach (FieldInfo field in ShadingFields())
            {
                ClampedFloatParameter own = gameplay != null ? (ClampedFloatParameter)field.GetValue(gameplay) : null;
                ClampedFloatParameter fallback = baseline != null ? (ClampedFloatParameter)field.GetValue(baseline) : null;
                ClampedFloatParameter used = own != null && own.overrideState ? own : fallback;
                ClampedFloatParameter range = own ?? fallback;

                text.Append(field.Name).Append(": ")
                    .Append(used != null ? Format(used.value) : "?")
                    .Append(" (gameplay ").Append(DescribeOverride(own))
                    .Append(" | baseline ").Append(fallback != null ? Format(fallback.value) : "none").Append(')');
                if (range != null)
                {
                    text.Append(" [").Append(Format(range.min)).Append(", ").Append(Format(range.max)).Append(']');
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        private static StationShading LoadShading(string profilePath)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                throw new InvalidOperationException($"No volume profile at {profilePath}.");
            }

            return profile.TryGet(out StationShading shading) ? shading : null;
        }

        private static ClampedFloatParameter ShadingParameter(StationShading shading, string parameter)
        {
            FieldInfo field = Array.Find(ShadingFields(), f => string.Equals(f.Name, parameter, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                string names = string.Join(", ", ShadingFields().Select(f => f.Name));
                throw new ArgumentException($"Station Shading has no parameter '{parameter}'. Parameters: {names}.", nameof(parameter));
            }

            return (ClampedFloatParameter)field.GetValue(shading);
        }

        private static FieldInfo[] ShadingFields()
        {
            return typeof(StationShading).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.FieldType == typeof(ClampedFloatParameter))
                .ToArray();
        }

        private static string DescribeOverride(ClampedFloatParameter parameter)
        {
            if (parameter == null)
            {
                return "none";
            }

            return parameter.overrideState ? Format(parameter.value) : "off";
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
