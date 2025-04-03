
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ItemPurchase : MonoBehaviour
{
    public Button applyButton;
    private NetworkPlayer networkPlayer;
    public StoreItem Item;
    public void DownloadItem()
    {
        if (GameManager.Instance.PurchaseItem(Item.Price))
        {
            GetComponent<Button>().enabled = false;
            GetComponent<Animator>().SetTrigger("Disabled");
            string internalUrl = Item.ThumbnailUrl;
            string filename = Item.Name.Replace(" ", "");
            string filepath = Path.Combine(Application.persistentDataPath, filename + "." + internalUrl.Split(".")[1]);
            FirebaseStorageManager.Instance.DownloadToFile(internalUrl, filepath);

            PlayerPrefs.SetString("PurchasedSkin", Item.Name);  // Save the skin name
            PlayerPrefs.Save();  // Don't forget to save PlayerPrefs
        }
        else
        {
            Debug.LogWarning("Insuffient balance!!");
        }

    }

    private void Start()
    {
        applyButton.onClick.AddListener(OnApplyButtonClicked); // Add listener for button click
    }

    private void OnApplyButtonClicked()
    {
        networkPlayer = NetworkPlayer.Instance;
        // Call the method to change skin when the player clicks the "Apply" button
        networkPlayer.ApplySkin(Item.Name);
    }
}