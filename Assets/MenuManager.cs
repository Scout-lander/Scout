using System.Collections.Generic;
using System.Linq;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Managing.Scened;
using FishNet.Object;
using HeathenEngineering.SteamworksIntegration.UI;

public class MenuManager : NetworkBehaviour
{
    [Header("Main")]
    [SerializeField] private GameObject mainMenuObject;
    [SerializeField] private TMP_InputField roomIDInput;
    [SerializeField] private Button joinRoomButton;

    [Header("Lobby")]
    [SerializeField] private GameObject lobbyObject;
    [SerializeField] private TextMeshProUGUI lobbyTitle;
    [SerializeField] private TextMeshProUGUI roomIDDisplay;
    [SerializeField] private Button startGameButton;
    [SerializeField] private LobbyManager lobbyManager;

    [Header("User Lobby Setup")]
    [SerializeField] private LobbyUserPanel lobbyUserPanelPrefab;
    [SerializeField] private Transform lobbyuserHolder;

    [Header("Network")]
    [SerializeField] private NetworkManager networkManager; // Reference to FishNet NetworkManager
    [SerializeField] private string gameSceneName = "Game"; // The name of the game scene to load

    [Header("Party Lobby Control")]
    [SerializeField] private PartyLobbyControl partyLobbyControl; // Reference to PartyLobbyControl


    private Dictionary<UserData, bool> _playerReadyStates = new(); // Track each player's ready state
    private Dictionary<UserData, LobbyUserPanel> _lobbyUserPanels = new();

    private void Awake()
    {
        OpenMainMenu();
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);

        UpdateStartButton();
        joinRoomButton.onClick.AddListener(JoinRoomByID);

        partyLobbyControl.OnPlayerReadyStatusChanged += OnUserReadyStatusChanged;
    }

    public void OnLobbyCreated(LobbyData lobbyData)
    {
        lobbyData.Name = UserData.Me.Name + "'s Lobby";
        lobbyTitle.text = lobbyData.Name;
        _playerReadyStates.Clear();
        //lobbyData.AllPlayersReady

        // Convert SteamId to a ulong and then to a shorter Base64 string for display
        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        ClearCards();
        OpenLobby();
        SetupCard(UserData.Me);
        UpdateStartButton();
    }

    private void OnDestroy()
    {
        if (partyLobbyControl != null)
            partyLobbyControl.OnPlayerReadyStatusChanged -= OnUserReadyStatusChanged;
    }

    public void OnLobbyJoined(LobbyData lobbyData)
    {
        ClearCards();
        lobbyTitle.text = lobbyData.Name;

        // Convert SteamId to a ulong and then to a shorter Base64 string for display
        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        OpenLobby();

        _playerReadyStates.Clear(); // Clear ready states when joining a new lobby
        foreach (var member in lobbyData.Members)
        {
            SetupCard(member.user);
            _playerReadyStates[member.user] = false; // Initialize all players as not ready
        }
        UpdateStartButton();
    }

    public void UpdateStartButton()
    {
        CheckAllPlayersReady();
    }

    public void OpenMainMenu()
    {
        CloseScreen();
        mainMenuObject.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameClientRpc()
    {
        networkManager.ClientManager.StartConnection();
        ChangeToGameScene();
    }

    public void OnUserReadyStatusChanged(UserData userData, bool isReady)
    {
        _playerReadyStates[userData] = isReady; // Update the user's ready status
    Debug.Log($"User: {userData.Name}, Ready Status: {isReady}");
        CheckAllPlayersReady(); // Re-evaluate if all players are ready
    }

    private void CheckAllPlayersReady()
    {
        bool allReady = _playerReadyStates.Values.All(ready => ready);
        startGameButton.gameObject.SetActive(lobbyManager.IsPlayerOwner && allReady);
    }

    public void OnStartGameButtonPressed()
    {
        if (lobbyManager.IsPlayerOwner && _playerReadyStates.Values.All(ready => ready))
        {
            Debug.Log("Starting the game as host...");
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();

            StartGameClientRpc();
            ChangeToGameScene();
        }
        else
        {
            Debug.LogWarning("Cannot start the game; not all players are ready.");
        }
    }

    public void ChangeToGameScene()
    {
        SceneLoadData sceneLoadData = new SceneLoadData(gameSceneName)
        {
            ReplaceScenes = ReplaceOption.All
        };

        networkManager.SceneManager.LoadGlobalScenes(sceneLoadData);
    }

    public void OpenLobby()
    {
        CloseScreen();
        lobbyObject.SetActive(true);
    }

    public void CloseScreen()
    {
        mainMenuObject.SetActive(false);
        lobbyObject.SetActive(false);
    }

    public void OverlayJoinButton(LobbyData lobbyData, UserData data)
    {
        lobbyManager.Join(lobbyData);
    }

    private void JoinRoomByID()
    {
        string shortRoomId = roomIDInput.text;
        try
        {
            // Convert the short code back to the original room ID
            ulong roomId = ConvertBase64ToRoomId(shortRoomId);
            lobbyManager.Join(roomId);
        }
        catch
        {
            Debug.LogError("Invalid Room ID entered.");
        }
    }

    public void OnUserJoin(UserData userData)
    {
        SetupCard(userData);
        _playerReadyStates[userData] = false; // Set the new user as not ready
        UpdateStartButton();
    }

    private void OnUserLeft(UserLobbyLeaveData userLeaveData)
    {
        if(!_lobbyUserPanels.TryGetValue(userLeaveData.user, out LobbyUserPanel panel))
        {
            Debug.LogError("Tried to remove user that doesn't exist");
            return;
        }

        Destroy(panel.gameObject);
        _lobbyUserPanels.Remove(userLeaveData.user);
        _playerReadyStates.Remove(userLeaveData.user); // Remove from ready states
        CheckAllPlayersReady(); // Re-evaluate if all players are ready
    }

    private void ClearCards()
    {
        foreach (Transform child in lobbyuserHolder)
        {
            Destroy(child.gameObject);
        }
        _lobbyUserPanels.Clear();
    }

    private void SetupCard(UserData userData)
    {
        var userPanel = Instantiate(lobbyUserPanelPrefab, lobbyuserHolder);
        userPanel.Initialize(userData);
        _lobbyUserPanels.TryAdd(userData, userPanel);
    }

    // Convert the room ID to a Base64 string for shorter display
    private string ConvertRoomIdToBase64(ulong roomId)
    {
        byte[] bytes = System.BitConverter.GetBytes(roomId);
        string base64String = System.Convert.ToBase64String(bytes)
                               .Replace("=", "") // Remove padding characters
                               .Replace("+", "-") // Replace '+' with '-' for URL safety
                               .Replace("/", "_"); // Replace '/' with '_'
        return base64String;
    }

    // Convert the shortened Base64 string back to the original room ID
    private ulong ConvertBase64ToRoomId(string base64String)
    {
        base64String = base64String
                       .Replace("-", "+")
                       .Replace("_", "/");
        byte[] bytes = System.Convert.FromBase64String(base64String + "=="); // Add padding if needed
        return System.BitConverter.ToUInt64(bytes, 0);
    }
}
