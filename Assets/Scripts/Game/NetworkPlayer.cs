using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
    {
    public static NetworkPlayer Instance;
    public NetworkVariable<string> currentSkin = new NetworkVariable<string>();
    public Image playerRenderer;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ApplySkin(string skinName)
    {
        if (IsOwner)
        {
            // Check if the skin has been purchased
            string purchasedSkin = PlayerPrefs.GetString("PurchasedSkin", "");

            if (purchasedSkin != skinName)
            {
                Debug.LogWarning($"Skin {skinName} has not been purchased yet!");
                return; // Exit the function if the skin hasn't been purchased
            }

            Debug.Log($"Player {OwnerClientId} applied skin: {skinName}");

            // Update the local player's skin immediately
            UpdatePlayerSkin(skinName);

            // Sync skin across the network
            ApplySkinServerRpc(skinName);
        }
    }


    [ServerRpc]
    private void ApplySkinServerRpc(string skinName)
    {
        currentSkin.Value = skinName; // Triggers OnSkinChanged for all players
    }


    private void UpdatePlayerSkin(string skinName)
    {
        string skinPathJpg = Path.Combine(Application.persistentDataPath, skinName + ".jpg");
        string skinPathJpeg = Path.Combine(Application.persistentDataPath, skinName + ".jpeg");
        string skinPath;

        // Check if the file exists with its original extension
        if (File.Exists(skinPathJpg))
        {
            skinPath = skinPathJpg;
        }
        else if (File.Exists(skinPathJpeg))
        {
            skinPath = skinPathJpeg;
        }
        else
        {
            Debug.LogError($"Skin file not found: {skinName} (checked .jpg and .jpeg)");
            return;
        }

        Debug.Log($"Loading skin from path: {skinPath}");

        byte[] fileData = File.ReadAllBytes(skinPath);
        Texture2D texture = new Texture2D(2, 2);

        if (texture.LoadImage(fileData))
        {
            Debug.Log($"Skin {skinName} loaded successfully.");
            playerRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError($"Failed to load texture: {skinPath}");
        }
    }


}

