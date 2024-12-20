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
    private int slots = 8;
    private ELobbyType lobbyType;
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private FishySteamworks.FishySteamworks fishySteamworks;

    private void Awake()
    {
        MainMenuUI.SetActive(true);
        lobbySetupPanel.SetActive(false);

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

        lobbyManager.evtEnterSuccess.AddListener(OnLobbyEntered);
        lobbyManager.evtEnterFailed.AddListener(OnLobbyJoinFailed);
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);
    }

    private void OnLobbyJoinFailed(EChatRoomEnterResponse result)
    {
        Debug.LogError($"Failed to join lobby. Error: {result}");
    }

    private void OverlayJoinButton(LobbyData lobbyData, UserData user)
    {
        Debug.Log($"Trying to join lobby with ID: {lobbyData.Owner}, Name: {lobbyData.Name}");
        Debug.Log($"Current members: {lobbyData.Members}, Max members: {lobbyData.MaxMembers}");

        if (lobbyManager != null)
        {
            

            lobbyManager.Join(lobbyData);
        }
        else
        {
            Debug.LogError("LobbyManager not found! Cannot join the lobby.");
        }
    }


    public void HostGame()
    {
        Debug.Log("Opening lobby setup screen...");
        lobbySetupPanel.SetActive(true); // Show lobby setup panel
    }

    private void OnLobbyEntered(LobbyData lobbyData)
    {
        Debug.Log($"Successfully joined lobby: {lobbyData.Name} with ID: {lobbyData.Owner}");
        SceneManager.LoadScene("LobbyScreen");
    }

    public void ConfirmLobbySetup()
    {
        // Fixed slots to 8
        slots = 8;

        // Gather settings from UI inputs
        lobbyName = lobbyNameInput.text;
        lobbyType = GetLobbyType(typeDropdown.value);

        Debug.Log($"Creating lobby: {lobbyName} | Slots: {slots} (fixed) | Type: {lobbyType}");

        // Set the LobbyManager's creation arguments
        lobbyManager.createArguments.name = lobbyName;
        lobbyManager.createArguments.slots = slots;
        lobbyManager.createArguments.type = lobbyType;

        // Use LobbyManager to create the lobby
        lobbyManager.Create();
        fishySteamworks.StartConnection(true); // Starts as server
    }

    public void OnLobbyCreated(LobbyData lobbyData)
    {
        Debug.Log("Lobby created successfully. Moving to lobby screen...");
        SceneManager.LoadScene("LobbyScreen"); // Load lobby scene
    }
    public void OtherUserJoin()
    {
        Debug.Log("LYou Have joint Lobby");
        SceneManager.LoadScene("LobbyScreen"); // Load lobby scene
    }
    public void OnLobbyJoin()
    {
        SceneManager.LoadScene("LobbyScreen"); // Load lobby scene
    }
    public void OnLobbyCreationFailed(LobbyData lobbyData)
    {
        Debug.LogError("Failed to create lobby.");
        // Optionally, show an error message to the player
    }

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
