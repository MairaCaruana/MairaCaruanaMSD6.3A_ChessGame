
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ItemPurchase : MonoBehaviour
{
    public StoreItem Item;
    public void DownloadItem()
    {
        if (GameManager.Instance.PurchaseItem(Item.Price))
        {
            GetComponent<Button>().enabled = false;
            GetComponent<Animator>().SetTrigger("Disabled");
            string internalUrl = Item.ThumbnailUrl.Split("firebasestorage.app/")[1];
            string filename = Item.Name.Replace(" ", "");
            string filepath = Path.Combine(Application.persistentDataPath, filename + "." + internalUrl.Split(".")[1]);
            FirebaseStorageManager.Instance.DownloadToFile(internalUrl, filepath);
        }
        else
        {
            Debug.LogWarning("Insuffient balance!!");
        }

    }
}