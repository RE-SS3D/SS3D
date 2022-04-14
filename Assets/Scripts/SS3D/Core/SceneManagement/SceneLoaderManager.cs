using System;
using System.Collections;
using Coimbra;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Core.SceneManagement
{
    /// <summary>
    /// Persistent object that loads and unloads scenes
    /// </summary>
    public sealed class SceneLoaderManager : MonoBehaviour
    {
        /// <summary>
        /// Tries to load a scene
        /// </summary>
        /// <param name="scene">A SceneReference of a scene</param>
        public void LoadScene(SceneReference scene, LoadSceneMode loadSceneMode)
        {
            UnloadScene(scene);
            StartCoroutine(LoadSceneCoroutine(scene, loadSceneMode));
        }

        /// <summary>
        /// Unloads the scene if it is loaded
        /// </summary>
        /// <param name="scene">A SceneReference of a scene</param>
        private static void UnloadScene(SceneReference scene)
        {
            if (SceneManager.GetSceneByName(scene).isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        /// <summary>
        /// Responsible for actually loading a scene
        /// </summary>
        /// <param name="scene">A SceneReference of a scene</param>
        /// <returns></returns>
        private IEnumerator LoadSceneCoroutine(SceneReference scene, LoadSceneMode loadSceneMode)
        {
            // if the scene is already loaded we unload it
            if (SceneManager.GetSceneByPath(scene.ScenePath).isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
            AsyncOperation operation = (SceneManager.LoadSceneAsync(scene, loadSceneMode));
            
            // waits until the scene loaded
            yield return new WaitUntil( () => operation.isDone);

            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(scene.ScenePath));
        }
    }
}
