using System.Collections.ObjectModel;

namespace SS3D.Traits
{
    public interface ITraitsHolder
    {
        public ReadOnlyCollection<Trait> Traits { get; }
    }
}
