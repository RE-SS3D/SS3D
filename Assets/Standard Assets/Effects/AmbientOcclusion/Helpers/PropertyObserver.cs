using UnityEngine;

namespace UnityStandardAssets.CinematicEffects
{
    public partial class AmbientOcclusion : MonoBehaviour
    {
        // Observer class that detects changes on properties
        class PropertyObserver
        {
            // AO properties
            bool _downsampling;
            OcclusionSource _occlusionSource;
            bool _ambientOnly;
            bool _debug;

            // Camera properties
            int _pixelWidth;
            int _pixelHeight;

            // Check if it has to reset itself for property changes.
            public bool CheckNeedsReset(Settings setting, Camera camera)
            {
                return
                    _downsampling != setting.downsampling ||
                    _occlusionSource != setting.occlusionSource ||
                    _ambientOnly != setting.ambientOnly ||
                    _debug != setting.debug ||
                    _pixelWidth != camera.pixelWidth ||
                    _pixelHeight != camera.pixelHeight;
            }

            // Update the internal state.
            public void Update(Settings setting, Camera camera)
            {
                _downsampling = setting.downsampling;
                _occlusionSource = setting.occlusionSource;
                _ambientOnly = setting.ambientOnly;
                _debug = setting.debug;
                _pixelWidth = camera.pixelWidth;
                _pixelHeight = camera.pixelHeight;
            }
        }
    }
}
