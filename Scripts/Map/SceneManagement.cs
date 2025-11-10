using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace ZombieGame.Map
{
    public class SceneManagement : NetworkBehaviour
    {
        public static SceneManagement Instance { get; private set; }

        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string loadingScene = "Loading";

        private NetworkVariable<bool> isLoading = new NetworkVariable<bool>(false);

        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LoadSceneServerRpc(string sceneName, ServerRpcParams rpcParams = default)
        {
            if (isLoading.Value) return;

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading.Value = true;
            OnSceneLoadStarted?.Invoke(sceneName);
            LoadSceneStartClientRpc(sceneName);

            // Load loading screen first
            yield return SceneManager.LoadSceneAsync(loadingScene, LoadSceneMode.Single);

            // Then load actual scene
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            
            while (!operation.isDone)
            {
                yield return null;
            }

            isLoading.Value = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            LoadSceneCompleteClientRpc(sceneName);
        }

        [ClientRpc]
        private void LoadSceneStartClientRpc(string sceneName)
        {
            OnSceneLoadStarted?.Invoke(sceneName);
        }

        [ClientRpc]
        private void LoadSceneCompleteClientRpc(string sceneName)
        {
            OnSceneLoadCompleted?.Invoke(sceneName);
        }

        public void LoadMainMenu()
        {
            NetworkManager.Singleton?.Shutdown();
            SceneManager.LoadScene(mainMenuScene);
        }

        public bool IsLoading => isLoading.Value;
    }
}
