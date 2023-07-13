using Coimbra;
using UnityEngine;

public class SpawnRuntimeEditor : MonoBehaviour
{
    [SerializeField] private GameObject _runtimeEditor;
    // Start is called before the first frame update
    void Start()
    {
        #if DEVELOPMENT_BUILD
        Instantiate(_runtimeEditor);
        #endif
        gameObject.Dispose(false);
    }
}
