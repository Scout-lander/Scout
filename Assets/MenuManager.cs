using System.Collections.Generic;
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
    [SerializeField] private LobbyUIControl partyLobbyControl; // Reference to PartyLobbyControl
    private HashSet<ulong> confirmedClients = new HashSet<ulong>(); // Track confirmed clients

    [Header("Join Game")]
    [SerializeField] private GameObject joinGameButton; // Reference to the "Join Game" button for clients

    private void Awake()
    {
        OpenMainMenu();
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);

        joinRoomButton.onClick.AddListener(JoinRoomByID);
        startGameButton.onClick.AddListener(OnStartGameButtonPressed);
        joinGameButton.SetActive(false); // Hide the join game button initially
    }

    public void OnLobbyCreated(LobbyData lobbyData)
    {
        lobbyData.Name = UserData.Me.Name + "'s Lobby";
        lobbyTitle.text = lobbyData.Name;

        string shortRoomId = ConvertRoomIdToBase64(lobbyData.SteamId.m_SteamID);
        roomIDDisplay.text = "Room ID: " + shortRoomId;

        OpenLobby();
        SetupCard(UserData.Me);
    }

    public void OnLobbyJoined(LobbyData lobbyData)
    {
        lobbyTitle.text = lobbyData.Name;

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
        if (lobbyManager.IsPlayerOwner)
        {
            Debug.Log("Starting the game as host...");
            
            // Start the server
            networkManager.ServerManager.StartConnection();

            // Start the host as a client
            networkManager.ClientManager.StartConnection();

            if (lobbyManager.Lobby.Members.Length == 1) // Solo case
            {
                ChangeToGameScene();
            }
            else
            {
                partyLobbyControl.SetLobbyGameReady(); // Set lobby to "game ready"
                ShowJoinGameButtonToClients(); // Notify clients to display the Join Game button
            }
        }
        else
        {
            Debug.LogWarning("Only the host can start the game.");
        }
}

    // Notify clients to display the "Join Game" button
    [ObserversRpc]
    private void ShowJoinGameButtonToClients()
    {
        Debug.Log("Displaying Join Game button for clients...");
        DisplayJoinGameButton(); // Clients will now see a button to join the game
    }

    // Method to handle "Join Game" button pressed by clients
    [ServerRpc(RequireOwnership = false)]
    public void JoinGameButtonPressed()
    {
        networkManager.ClientManager.StartConnection();
        ConfirmClientConnection(UserData.Me.SteamId);
    }

    // On client side, displays the Join Game button
    private void DisplayJoinGameButton()
    {
        joinGameButton.SetActive(true);
        joinGameButton.GetComponent<Button>().onClick.AddListener(() => JoinGameButtonPressed());
    }

    private void ConfirmClientConnection(ulong clientId)
    {
        confirmedClients.Add(clientId);

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
        joinGameButton.SetActive(false); // Ensure join game button is hidden when switching screens
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

    private string ConvertRoomIdToBase64(ulong roomId)
    {
        byte[] bytes = System.BitConverter.GetBytes(roomId);
        string base64String = System.Convert.ToBase64String(bytes)
                               .Replace("=", "")
                               .Replace("+", "-")
                               .Replace("/", "_");
        return base64String;
    }

    private ulong ConvertBase64ToRoomId(string base64String)
    {
        base64String = base64String
                       .Replace("-", "+")
                       .Replace("_", "/");
        byte[] bytes = System.Convert.FromBase64String(base64String + "==");
        return System.BitConverter.ToUInt64(bytes, 0);
    }
}
