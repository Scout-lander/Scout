using HeathenEngineering.SteamworksIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUserPanel : MonoBehaviour
{
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Image panelBackground;

    public bool IsReady { get; private set; } // Tracks if the player is ready

    public void Initialize(UserData userData)
    {
        userData.LoadAvatar(SetAvatar);
        nameText.text = userData.Name;
        
        // Set up the button to toggle ready state
        readyButton.onClick.AddListener(ToggleReadyState);
        SetReady(false); // Initialize as not ready
    }

    private void SetAvatar(Texture2D userImage)
    {
        avatarImage.texture = userImage;
    }

    private void ToggleReadyState()
    {
        SetReady(!IsReady);
        
        // Notify MenuManager of the change in ready state
        MenuManager.Instance.OnPlayerReadyStateChanged(this);
    }

    public void SetReady(bool ready)
    {
        IsReady = ready;
        panelBackground.color = ready ? Color.green : Color.grey; // Change background color based on ready state
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text = ready ? "Unready" : "Ready"; // Update button text
    }
}
