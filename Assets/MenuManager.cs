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

    private void Awake()
    {
        OpenMainMenu();
        HeathenEngineering.SteamworksIntegration.API.Overlay.Client.EventGameLobbyJoinRequested.AddListener(OverlayJoinButton);

        UpdateStartButton();
        joinRoomButton.onClick.AddListener(JoinRoomByID);
        
       
    }

    public void OnLobbyCreated(LobbyData lobbyData)
    {
        lobbyData.Name = UserData.Me.Name + "'s Lobby";
        lobbyTitle.text = lobbyData.Name;
        roomIDDisplay.text = "Room ID: " + lobbyData.SteamId.ToString();
        
        ClearCards();
        OpenLobby();
        SetupCard(UserData.Me);
        UpdateStartButton();
    }

    public void OnLobbyJoined(LobbyData lobbyData)
    {
        ClearCards();
        lobbyTitle.text = lobbyData.Name;
        roomIDDisplay.text = "Room ID: " + lobbyData.SteamId.ToString();
        
        OpenLobby();
        UpdateStartButton();
        //SetupCard(UserData.Me);

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
        if (ulong.TryParse(roomIDInput.text, out ulong roomId))
        {
            lobbyManager.Join(roomId);
        }
        else
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
        _lobbyUserPanels.TryAdd(userData, userPanel);
    }
}
