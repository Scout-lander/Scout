using UnityEngine;
using UnityEngine.SceneManagement;
using HeathenEngineering.SteamworksIntegration;
using FishySteamworks;
using FishNet.Managing;

public class BootstrapManager : MonoBehaviour
{
    private FishySteamworks.FishySteamworks fishySteamworks;
    private NetworkManager networkManager;

    private void Awake()
    {
        // Set the BootstrapManager, NetworkManager, and FishySteamworks to persist across scenes
        DontDestroyOnLoad(gameObject);
        
        // Ensure NetworkManager and FishySteamworks are set to persist as well
        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            DontDestroyOnLoad(networkManager.gameObject);
        }
        else
        {
            Debug.LogError("NetworkManager not found in Bootstrap scene.");
        }

        fishySteamworks = FindObjectOfType<FishySteamworks.FishySteamworks>();
        if (fishySteamworks != null)
        {
            DontDestroyOnLoad(fishySteamworks.gameObject);
        }
        else
        {
            Debug.LogError("FishySteamworks not found in Bootstrap scene.");
        }
    }

    public void OnSteamInitialized()
    {
        Debug.Log("Steam initialized successfully. Moving to Main Menu.");
        
        SceneManager.LoadScene("MainMenu"); // Load the Main Menu scene
    }

    public void OnSteamInitializationError()
    {
        Debug.LogError("Failed to initialize Steam.");

    }
}
