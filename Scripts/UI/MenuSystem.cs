using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace ZombieGame.UI
{
    public class MenuSystem : MonoBehaviour
    {
        public static MenuSystem Instance { get; private set; }

        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;

        private bool isPaused = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }
        }

        public void TogglePauseMenu()
        {
            if (mainMenuPanel != null && mainMenuPanel.activeSelf) return;

            isPaused = !isPaused;
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(isPaused);
            }

            Time.timeScale = isPaused ? 0f : 1f;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }

        public void StartHost()
        {
            NetworkManager.Singleton?.StartHost();
            HideMainMenu();
        }

        public void StartClient()
        {
            NetworkManager.Singleton?.StartClient();
            HideMainMenu();
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            NetworkManager.Singleton?.Shutdown();
            SceneManager.LoadScene(0);
        }

        private void HideMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
    }
}
