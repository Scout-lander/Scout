using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeathenEngineering.SteamworksIntegration;
using FishNet.Managing;
using Steamworks;
using TMPro;
using FishySteamworks;
using System;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject MainMenuUI;

    [Header("Lobby Setup UI")]
    public GameObject lobbySetupPanel;
    public TMP_InputField lobbyNameInput;  // TextMeshPro InputField
    public TMP_Dropdown slotDropdown;      // TextMeshPro Dropdown
    public TMP_Dropdown typeDropdown;      // TextMeshPro Dropdown
    public Button confirmButton;
    

    private string lobbyName;
    private int slots;
    private ELobbyType lobbyType;
    [SerializeField]private LobbyManager lobbyManager; 
    [SerializeField]private NetworkManager networkManager;
    [SerializeField] private FishySteamworks.FishySteamworks fishySteamworks;

    private void Awake()
    {
        // Attempt to find or assign NetworkManager
        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found in the scene. Ensure it's set in the Bootstrap scene and marked as DontDestroyOnLoad.");
        }

        // Attempt to find or assign FishySteamworks
        fishySteamworks = FindObjectOfType<FishySteamworks.FishySteamworks>();
        if (fishySteamworks == null)
        {
            Debug.LogError("FishySteamworks not found in the scene. Ensure it's set in the Bootstrap scene and marked as DontDestroyOnLoad.");
        }

        // Attempt to find or assign LobbyManager
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager not found in the scene.");
        }

        // Listen for game lobby invites and join requests
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);
    }

    // Method to handle Steam overlay "Join Game" button press
    public void OverlayJoinButton(LobbyData lobbyData, UserData user)
    {
        Debug.Log("Join request via Steam overlay received.");
        JoinLobby(lobbyData);
    }

    // Method to handle Steam lobby invites
    private void OnLobbyInviteReceived(LobbyData lobbyData)
    {
        Debug.Log("Lobby invite received. Joining lobby...");
        JoinLobby(lobbyData);
    }

    // Method to join the specified lobby
    private void JoinLobby(LobbyData lobbyData)
    {
        if (lobbyManager != null)
        {
            lobbyManager.Join(lobbyData);
        }
        else
        {
            Debug.LogError("LobbyManager not found. Unable to join the lobby.");
        }
    }
    private void Start()
    {
        MainMenuUI.SetActive(true);
        lobbySetupPanel.SetActive(false); // Hide lobby setup initially
    }

    // Method to open the lobby settings screen
    public void HostGame()
    {
        Debug.Log("Opening lobby setup screen...");
        lobbySetupPanel.SetActive(true); // Show lobby setup panel
    }

    // Method to confirm lobby setup and create the lobby
    public void ConfirmLobbySetup()
    {
        // Gather settings from UI inputs
        lobbyName = lobbyNameInput.text;
        slots = int.Parse(slotDropdown.options[slotDropdown.value].text);
        lobbyType = GetLobbyType(typeDropdown.value);

        Debug.Log($"Creating lobby: {lobbyName} | Slots: {slots} | Type: {lobbyType}");

        // Start the FishNet server
       // networkManager.ServerManager.StartServer();

        // Set the LobbyManager's creation arguments
        lobbyManager.createArguments.name = lobbyName;
        lobbyManager.createArguments.slots = slots;
        lobbyManager.createArguments.type = lobbyType;

        // Use LobbyManager to create the lobby
        lobbyManager.Create();
        fishySteamworks.StartConnection(true); //Starts as server
        //fishySteamworks.StartConnection(false); //joins as client

    }

    // Event handler for successful lobby creation
    public void OnLobbyCreated(LobbyData lobbyData)
    {
        Debug.Log("Lobby created successfully. Moving to lobby screen...");
        SceneManager.LoadScene("LobbyScreen"); // Load lobby scene
    }

    // Event handler for lobby creation failure
    public void OnLobbyCreationFailed(LobbyData lobbyData)
    {
        Debug.LogError("Failed to create lobby.");
        // Optionally, show an error message to the player
    }

    // Helper method to map dropdown selection to ELobbyType
    private ELobbyType GetLobbyType(int index)
    {
        switch (index)
        {
            case 0: return ELobbyType.k_ELobbyTypePublic;
            case 1: return ELobbyType.k_ELobbyTypeFriendsOnly;
            case 2: return ELobbyType.k_ELobbyTypePrivate;
            default: return ELobbyType.k_ELobbyTypeInvisible;
        }
    }
}
