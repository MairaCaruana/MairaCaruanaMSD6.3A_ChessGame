using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using System.Xml.Serialization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "StoreItems", menuName = "ScriptableObjects/StoreItems", order = 1)]
public class StoreItemsSO : ScriptableObject
{
    [TableList(AlwaysExpanded = true)]
    [OnValueChanged(nameof(OnStoreItemListChanged))]
    public List<StoreItem> Items = new List<StoreItem>();

    [ShowInInspector]
    [DictionaryDrawerSettings(KeyLabel = "Item ID", ValueLabel = "Thumbnail",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [SpritePreviewInDictionary]
    public Dictionary<string, Sprite> Thumbnails = new Dictionary<string, Sprite>();

    [ShowInInspector]
    public GameObject StoreItemPrefab;

    private HashSet<string> existingIds = new HashSet<string>();
    private void OnStoreItemListChanged()
    {
        //Loop through each item in the list
        foreach (var item in Items)
        {
            //If Item ID is not set we create one
            if (string.IsNullOrEmpty(item.ID))
            {
                item.ID = GenerateUniqueID();
                existingIds.Add(item.ID);
            }
            //If Item ID is not in the Dictionary we create one
            if (!Thumbnails.ContainsKey(item.ID))
            {
                Thumbnails[item.ID] = null;
            }
        }
    }

    public string GenerateUniqueID()
    {
        string newId;
        //loop until a unique ID is found
        do
        {
            newId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        } while (existingIds.Contains(newId));
        return newId;
    }

    [Button("Export To XML")]
    private void ExportToXML()
    {
        //define the path to save the XML file
        string path = Path.Combine(Application.dataPath, "StoreItems.xml");

        try
        {
            //Serialize the Items list to XML
            XmlSerializer serializer = new XmlSerializer(typeof(List<StoreItem>));

            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                serializer.Serialize(writer, Items);
            }

            Debug.Log($"StoreItems exported to XML successfully at: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to export StoreItems to XML. Error: {ex.Message}");
        }
    }

    [Button("Simulate Shop Items")]
    private void SimulateShopItems()
    {
        Transform parent = GameObject.Find("ShopItems").GetComponent<Transform>();
        foreach (StoreItem item in Items)
        {
            GameObject newStoreitem = Instantiate(StoreItemPrefab, parent);
            //1 sprite, 3 price, 4 name
            newStoreitem.transform.GetChild(1).GetComponent<Image>().sprite = Thumbnails[item.ID];
            newStoreitem.transform.GetChild(3).GetComponent<TMP_Text>().text = item.Price.ToString();
            newStoreitem.transform.GetChild(4).GetComponent<TMP_Text>().text = item.Name;
        }
    }
    [Button("Upload XML File")]
    private void UploadXMLFile()
    {
        FirebaseStorageManager.Instance.UploadFileToStorage(Path.Combine(Application.dataPath, "StoreItems.xml"), "StoreItems.xml");
    }

    [Button("Download DLC Test")]
    private void DownloadDLCTest()
    {
        FirebaseStorageManager.Instance.DownloadToByteArray("StoreItems.xml", FirebaseStorageManager.DownloadType.MANIFEST);
    }
}