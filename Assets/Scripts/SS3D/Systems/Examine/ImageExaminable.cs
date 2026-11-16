using UnityEngine;

namespace SS3D.Systems.Examine
{
    public class ImageExaminable : ExaminableBase, IImageExaminable
    {
        [SerializeField] private Sprite detailedImage;

        public Sprite GetDetailedImage()
        {
            return detailedImage;
        }
    }
}
