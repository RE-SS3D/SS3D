using NUnit.Framework;
using SS3D.Systems.Audio;
using UnityEngine;

namespace EditorTests
{
    public class BlastAudioAirTests
    {
        [Test]
        public void HeldAir_OutlastsTheRoomVenting_UntilTheNextPlay()
        {
            GameObject host = new("Blast voice", typeof(AudioSource));
            float listenerAir = AudioSourceOcclusion.ListenerAir;
            try
            {
                AudioSourceOcclusion occlusion = host.AddComponent<AudioSourceOcclusion>();
                AudioSourceOcclusion.ListenerAir = 1f;
                occlusion.PrepareForPlayback(1f);
                occlusion.HoldStartAir();

                AudioSourceOcclusion.ListenerAir = 0f;
                Assert.AreEqual(1f, occlusion.Air, "The boom keeps the air it started in");

                occlusion.PrepareForPlayback(1f);
                Assert.AreEqual(0f, occlusion.Air, "The pooled source's next clip hears the vacuum");
            }
            finally
            {
                AudioSourceOcclusion.ListenerAir = listenerAir;
                Object.DestroyImmediate(host);
            }
        }
    }
}
