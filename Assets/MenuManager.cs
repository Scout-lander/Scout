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
    private HashSet<ulong> confirmedClients = new HashSet<ulong>(); // Track confirmed clients



    private void Awake()
    {
        OpenMainMenu();
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);

        joinRoomButton.onClick.AddListener(JoinRoomByID);


    }

    public void OnLobbyCreated(LobbyData lobbyData)
    {
        lobbyData.Name = UserData.Me.Name + "'s Lobby";
        lobbyTitle.text = lobbyData.Name;

        // Convert SteamId to a ulong and then to a shorter Base64 string for display
        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        OpenLobby();
        SetupCard(UserData.Me);
    }


    public void OnLobbyJoined(LobbyData lobbyData)
    {
        lobbyTitle.text = lobbyData.Name;

        // Convert SteamId to a ulong and then to a shorter Base64 string for display
        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        OpenLobby();
    }

    public void OpenMainMenu()
    {
        CloseScreen();
        mainMenuObject.SetActive(true);
    }

    public void OnStartGameButtonPressed()
    {
        if (lobbyManager.IsPlayerOwner && partyLobbyControl.allReady)
        {
            Debug.Log("Starting the game as host...");
            networkManager.ServerManager.StartConnection();

            if (lobbyManager.Lobby.Members.Length == 1) // Solo case
            {
                ChangeToGameScene();
            }
            else
            {
                partyLobbyControl.SetLobbyGameReady(); // Set lobby to "game ready"
                NotifyClientsToStartConnection();
            }
        }
        else
        {
            Debug.LogWarning("Cannot start the game; not all players are ready.");
        }
    }

    [ObserversRpc]
    private void NotifyClientsToStartConnection()
    {
        Debug.Log("Notifying clients to connect to FishNet...");
        StartGameClientRpc(); // Clients will now connect and change scenes
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameClientRpc()
    {
        networkManager.ClientManager.StartConnection();
        ChangeToGameScene();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmClientConnection(ulong clientId)
    {
        confirmedClients.Add(clientId);
        ChangeToGameScene();

        if (confirmedClients.Count == lobbyManager.Lobby.Members.Length)
        {
            Debug.Log("All clients confirmed. Starting the game...");
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



    private void SetupCard(UserData userData)
    {
        var userPanel = Instantiate(lobbyUserPanelPrefab, lobbyuserHolder);
        userPanel.Initialize(userData);
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
