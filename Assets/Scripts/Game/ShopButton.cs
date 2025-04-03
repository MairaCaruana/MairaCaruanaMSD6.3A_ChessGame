using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    public GameObject shopUICanvas; // Reference to the Shop UI Canvas
    public Button exitButton; // Reference to the Exit Button

    void Start()
    {
        // Set up the exit button functionality if it's assigned
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonPressed);
        }
    }

    public void OnShopButtonPressed()
    {
        // Set the shop UI Canvas to active when the shop button is pressed
        shopUICanvas.SetActive(true);
    }

    public void OnExitButtonPressed()
    {
        // Set the shop UI Canvas to inactive when the exit button is pressed
        shopUICanvas.SetActive(false);
    }
}
