using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ErrorPopup : MonoBehaviour
{
    public GameObject popupPanel; // The panel to show the error message
    public TextMeshProUGUI errorMessage; // Text component for the error message
    public Button closeButton; // Button to close the application

    void Start()
    {
        // Ensure the popup is hidden at start
        popupPanel.SetActive(false);

        // Add listener to the close button to exit the application
        closeButton.onClick.AddListener(CloseApplication);
    }

    public void ShowError(string message)
    {
        errorMessage.text = message;
        popupPanel.SetActive(true);
    }

    void CloseApplication()
    {
        // Exits the game (will only work in a built application, not in the editor)
        Application.Quit();
    }
}
