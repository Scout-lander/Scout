using System.Collections.Generic;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Managing.Scened;
using FishNet.Object;

public class MenuManager : NetworkBehaviour
{
    public static MenuManager Instance { get; private set; } // Singleton reference

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

    private Dictionary<UserData, LobbyUserPanel> _lobbyUserPanels = new();
    private Dictionary<UserData, bool> playerReadyStatus = new();


    private void Awake()
    {
        Instance = this; // Set singleton instance
        OpenMainMenu();
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);

        UpdateStartButton();
        joinRoomButton.onClick.AddListener(JoinRoomByID);
    }

     public void OnLobbyCreated(LobbyData lobbyData)
    {
        lobbyData.Name = UserData.Me.Name + "'s Lobby";
        lobbyTitle.text = lobbyData.Name;

        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        ClearCards();
        OpenLobby();
        SetupCard(UserData.Me);
        UpdateStartButton();
    }

    public void OnLobbyJoined(LobbyData lobbyData)
    {
        ClearCards();
        lobbyTitle.text = lobbyData.Name;

        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        OpenLobby();
        UpdateStartButton();

        foreach (var member in lobbyData.Members)
            SetupCard(member.user);
    }

     public void UpdateStartButton()
    {
        startGameButton.gameObject.SetActive(lobbyManager.IsPlayerOwner);
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

    public void OnStartGameButtonPressed()
    {
        if (lobbyManager.IsPlayerOwner)
        {
            Debug.Log("Starting the game as host...");
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();

            StartGameClientRpc();
            ChangeToGameScene();
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
        _lobbyUserPanels[userData] = userPanel;
        playerReadyStatus[userData] = false; // Initially mark each player as not ready
    }

    // Called by LobbyUserPanel when a player toggles their ready state
    public void OnPlayerReadyStateChanged(LobbyUserPanel userPanel)
    {
        // Find the user associated with this panel and update their ready status
        foreach (var entry in _lobbyUserPanels)
        {
            if (entry.Value == userPanel)
            {
                playerReadyStatus[entry.Key] = userPanel.IsReady;
                CheckAllPlayersReady();
                break;
            }
        }
    }

    private void CheckAllPlayersReady()
    {
        bool allReady = true;

        // Check if all players are ready
        foreach (bool isReady in playerReadyStatus.Values)
        {
            if (!isReady)
            {
                allReady = false;
                break;
            }
        }

        // Enable the start game button only if all players are ready and the local player is the host
        startGameButton.interactable = allReady && lobbyManager.IsPlayerOwner;
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
