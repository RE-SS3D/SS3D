using UnityEngine;
namespace SS3D.Core
{
    public interface IActor
    {
        public int Id { get; }

        public GameObject GameObject { get; }
    }
}