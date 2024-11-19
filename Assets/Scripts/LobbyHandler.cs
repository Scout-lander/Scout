using UnityEngine;
using HeathenEngineering.SteamworksIntegration;
using FishySteamworks;
using UnityEngine.SceneManagement;
using HeathenEngineering.SteamworksIntegration.UI;

public class LobbyHandler : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager; // Reference to the Lobby Manager
    private FishySteamworks.FishySteamworks fishySteamworks;
    [SerializeField] private LobbyUIControl partyLobbyControl;

    public bool isHost;

    public LobbyData Lobby
        {
            get;
            set;
        }


    private void Start()
    {
        // Find and assign the FishySteamworks instance in the scene
        fishySteamworks = FindObjectOfType<FishySteamworks.FishySteamworks>();
        if (fishySteamworks == null)
        {
            Debug.LogError("fishySteamworks not found in the scene.");
        }
        
        // Find and assign the PartyLobbyControl instance in the scene
        partyLobbyControl = FindObjectOfType<LobbyUIControl>();
        if (partyLobbyControl == null)
        {
            Debug.LogError("PartyLobbyControl not found in the scene.");
        }
        else
        {
            // Set isHost based on PartyLobbyControl's IsPlayerOwner property
            isHost = partyLobbyControl.IsPlayerOwner;
             // Retrieve and log the host's nickname
                var hostLobbyMemberData = partyLobbyControl.Lobby.Owner; // Owner is the lobby host
                Debug.Log($"The host is: {hostLobbyMemberData.user.Name}");

        }

        // Check if the current player is the host
        if (isHost)
        {
            Debug.Log("I am the lobby host.");
            // Start the server or perform other host-specific actions
            fishySteamworks.StartConnection(true); // Start as server
            fishySteamworks.StartConnection(false); // Connect as a client
        }
        else
        {
            ConnectToServerAsClient();
        }
    }

    private void ConnectToServerAsClient()
    {
        Debug.Log("Connecting to FishyNet server as client...");
        fishySteamworks.StartConnection(false); // Connect as a client
    }

    private void Update()
    {
        // Check if the 'P' key is pressed
        if (Input.GetKeyDown(KeyCode.P))
        {
            ShowOwner();
        }
    }

    // Method to log the owner's name
    public void ShowOwner()
    {

            var hostLobbyMemberData = partyLobbyControl.Lobby.Owner;
            Debug.Log($"The host is: {hostLobbyMemberData.user.Name}");

    }

    // Method called when a player joins the lobby
    public void OnPlayerJoined(LobbyMemberData memberData)
    {
        Debug.Log($"Player {memberData.user.Nickname} joined the lobby.");
        
        if (!isHost)
        {
            ConnectToServerAsClient();
        }
    }

    // Method called when a player leaves the lobby
    public void OnPlayerLeft(UserData userData)
    {
        Debug.Log($"Player {userData.Nickname} left the lobby.");
        // You might want to handle player disconnects here if necessary
    }

    // Method to leave the lobby and return to the main menu
    public void OnLobbyLeave()
    {
        Debug.Log("Leaving the lobby and returning to the main menu.");
        
        // Disconnect from the Fishy server if needed
        fishySteamworks.StopConnection(true);

        // Return to the MainMenu scene
        SceneManager.LoadScene("MainMenu");
    }
    
}
