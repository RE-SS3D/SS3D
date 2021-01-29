using UnityEngine;

[CreateAssetMenu(fileName = "Trait", menuName = "Inventory/Trait")]
public class Trait : ScriptableObject
{
    //Hash for identification
    protected int hash;
    [HideInInspector] public int Hash
    {
        get => hash;
        set => hash = value;
    }

    protected bool Equals(Trait other)
    {
        return hash == other.hash;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Trait) obj);
    }

    public override int GetHashCode()
    {
        return hash;
    }

    [ExecuteInEditMode]
    private void OnValidate()
    {
        hash = Animator.StringToHash(name.ToUpper());
    }
}
