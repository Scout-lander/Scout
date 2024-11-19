using UnityEngine;
using Steamworks;
using TMPro;

public class SteamUserDisplay : MonoBehaviour
{
    public TMP_Text userNameText; // Reference to a TextMeshPro Text UI component

    private void Start()
    {

            // Get the Steam username
            string steamUserName = SteamFriends.GetPersonaName();
            // Display the username in the TextMeshPro Text UI component
            if (userNameText != null)
            {
                userNameText.text = "Welcome, " + steamUserName;
            }
            else
            {
                Debug.LogWarning("userNameText is not assigned in the inspector.");
            }

    }
}
