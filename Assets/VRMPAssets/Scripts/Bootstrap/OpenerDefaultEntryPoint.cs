using UnityEngine;
using UnityEngine.SceneManagement;

namespace XRMultiplayer
{
    /// <summary>
    /// Forces the opener scene to be the default startup scene in player builds.
    /// </summary>
    public static class OpenerDefaultEntryPoint
    {
        const string k_OpenerSceneName = "OpenerScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureDefaultScene()
        {
            if (Application.isEditor)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == k_OpenerSceneName)
                return;

            SceneManager.LoadScene(k_OpenerSceneName, LoadSceneMode.Single);
        }
    }
}
