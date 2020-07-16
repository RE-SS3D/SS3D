using UnityEngine;

[CreateAssetMenu(fileName = "Trait", menuName = "Inventory/Trait")]
public class Trait : ScriptableObject
{
    //Hash for identification
    protected int hash;
    [HideInInspector] public int Hash => hash;

    [ExecuteInEditMode]
    private void OnValidate()
    {
        hash = Animator.StringToHash(name.ToUpper());
    }
}
