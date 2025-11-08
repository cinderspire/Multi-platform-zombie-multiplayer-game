using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;

namespace DeadFrontier.Networking
{
    /// <summary>
    /// Initializes Unity Gaming Services and handles authentication
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;

        private bool isInitialized = false;

        public bool IsInitialized => isInitialized;

        private async void Start()
        {
            if (autoInitialize)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// Initializes Unity Gaming Services
        /// </summary>
        public async Task InitializeAsync()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[NetworkBootstrap] Already initialized");
                return;
            }

            try
            {
                Debug.Log("[NetworkBootstrap] Initializing Unity Gaming Services...");

                // Initialize Unity Services
                await Unity.Services.Core.UnityServices.InitializeAsync();

                // Sign in anonymously
                if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                {
                    await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[NetworkBootstrap] Signed in as: {Unity.Services.Authentication.AuthenticationService.Instance.PlayerId}");
                }

                isInitialized = true;
                Debug.Log("[NetworkBootstrap] Initialization complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkBootstrap] Initialization failed: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the current player ID
        /// </summary>
        public string GetPlayerId()
        {
            if (!isInitialized)
                return null;

            return Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        }
    }
}
