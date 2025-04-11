using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Storage;
using UnityEngine;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using UnityEngine.UI;
using Firebase;

public class FirebaseStorageManager : MonoBehaviour
{
    private FirebaseStorage _storage;
    private StorageReference _storageRef;
    public static FirebaseStorageManager Instance;
    public GameObject StoreItemPrefab;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                _storage = FirebaseStorage.DefaultInstance;
                _storageRef = _storage.RootReference;

                // Load the Store
                DownloadToByteArray("StoreItems.xml", FirebaseStorageManager.DownloadType.MANIFEST);
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }
        });
    }

    public void UploadFileToStorage(string path, string filename)
    {
        StorageReference storeItemsRef = _storageRef.Child(filename);
        storeItemsRef.PutFileAsync(path)
            .ContinueWith((Task<StorageMetadata> task) =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.Log(task.Exception.ToString());
                }
                else
                {
                    // Metadata contains file metadata such as size, content-type, and download URL.
                    StorageMetadata metadata = task.Result;
                    string md5Hash = metadata.Md5Hash;
                    Debug.Log("Finished uploading...");
                    Debug.Log("md5 hash = " + md5Hash);
                }
            });
    }

    public enum DownloadType { IMAGE = 0, MANIFEST = 1 }
    public void DownloadToByteArray(string filename, DownloadType downloadType, StoreItem storeItem = null)
    {
        StorageReference storeItemsRef = _storageRef.Child(filename);
        // Download in memory with a maximum allowed size of 1MB (1 * 1024 * 1024 bytes)
        const long maxAllowedSize = 1 * 1024 * 1024;
        storeItemsRef.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogException(task.Exception);
                // Uh-oh, an error occurred!
            }
            else
            {
                byte[] fileContents = task.Result;
                switch (downloadType)
                {
                    case DownloadType.IMAGE:
                        StartCoroutine(LoadImageContainer(fileContents, storeItem));
                        break;
                    case DownloadType.MANIFEST:
                        StartCoroutine(LoadManifest((fileContents)));
                        break;
                }
                Debug.Log("Finished downloading!");
            }
        });
    }

    IEnumerator LoadManifest(byte[] byteArr)
    {
        string manifestData = System.Text.Encoding.UTF8.GetString(byteArr);
        string[] lines = manifestData.Split('\n');
        string remainingData = string.Join("\n", lines.Skip(1));

        XDocument manifest = XDocument.Parse(remainingData);
        foreach (XElement element in manifest.Root.Elements())
        {
            StoreItem item = new StoreItem();
            // Extract data from each child element
            item.ID = element.Element("ID").Value;
            item.Name = element.Element("Name").Value;
            item.ThumbnailUrl = element.Element("ThumbnailUrl").Value;
            float price;
            if (float.TryParse(element.Element("Price").Value, out price))
            {
                item.Price = price;
            }
            else
            {
                Debug.LogError("Failed to parse Price for item: " + element.Element("Name").Value);
            }

            float discount;
            if (float.TryParse(element.Element("Discount").Value, out discount))
            {
                item.Discount = discount;
            }
            else
            {
                Debug.LogError("Failed to parse Discount for item: " + element.Element("Name").Value);
            }

            DownloadToByteArray(item.ThumbnailUrl, DownloadType.IMAGE, item);

        }
        yield return null;
    }


    IEnumerator LoadImageContainer(byte[] byteArr, StoreItem storeItem)
    {
        if (storeItem == null)
        {
            Debug.LogError("StoreItem is null in LoadImageContainer!");
            yield break;
        }

        //Instantiating the store items
        Texture2D imageTexture = new Texture2D(1, 1);

        if (!imageTexture.LoadImage(byteArr))
        {
            Debug.LogError("Failed to load image from byte array.");
            yield break;
        }
        //converting the byte array into a texture
      //  imageTexture.LoadImage(byteArr);

        Transform parent = GameObject.Find("ShopItems").GetComponent<Transform>();
        if (parent == null)
        {
            Debug.LogError("Could not find GameObject named 'ShopItems'.");
            yield break;
        }
        GameObject newStoreitem = Instantiate(StoreItemPrefab, parent);
        if (newStoreitem == null)
        {
            Debug.LogError("Failed to instantiate StoreItemPrefab.");
            yield break;
        }
        newStoreitem.transform.GetChild(1).GetComponent<RawImage>().texture = imageTexture;
        newStoreitem.transform.GetChild(3).GetComponent<TMP_Text>().text = storeItem.Price.ToString();
        newStoreitem.transform.GetChild(4).GetComponent<TMP_Text>().text = storeItem.Name;
        newStoreitem.GetComponent<ItemPurchase>().Item = storeItem;
        yield return null;
    }

    public void DownloadToFile(string url, string filepath)
    {
        // Create local filesystem URL
        StorageReference storeItemsRef = _storageRef.Child(url);
        // Download to the local filesystem
        storeItemsRef.GetFileAsync(filepath).ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && !task.IsCanceled)
            {
                Debug.Log($"File downloaded to: {filepath}");
            }
        });
    }


}
