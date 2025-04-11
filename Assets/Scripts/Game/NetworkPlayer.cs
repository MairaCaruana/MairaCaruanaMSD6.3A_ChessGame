using System.IO;
using Unity.Collections;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
    {
    public static NetworkPlayer Instance;
    private NetworkObject netObj;

    public NetworkVariable<FixedString64Bytes> currentSkin = new NetworkVariable<FixedString64Bytes>();
    public Image playerRenderer;

    public NetworkVariable<PlayerRole> playerRole = new NetworkVariable<PlayerRole>(PlayerRole.None);

    public Side PlayerSide => playerSide.Value;

    private NetworkVariable<Side> playerSide = new NetworkVariable<Side>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log($"[Client {OwnerClientId}] NetworkPlayer spawned. My side is: {playerSide.Value}");
            playerSide.OnValueChanged += OnPlayerSideChanged;
        }
    }

    private void OnPlayerSideChanged(Side previous, Side current)
    {
        Debug.Log($"[Client {OwnerClientId}] Player side changed from {previous} to {current}");
    }

    private void Awake()
    {
        netObj = GetComponent<NetworkObject>();


        if (Instance == null)
        {
            Instance = this;
        }

        // Ensure network object is spawned for both client and host
        if (IsOwner)
        {
            netObj.Spawn(); // Spawn the network object if it's the player's turn to own it
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int startFile, int startRank, int endFile, int endRank, ElectedPiece promotionType,
   ServerRpcParams rpcParams = default)
    {
        if (IsServer)
        {
            Square start = new Square(startFile, startRank);
            Square end = new Square(endFile, endRank);

            Transform movedPiece = BoardManager.Instance.GetPieceGOAtPosition(start)?.transform;
            Transform targetSquare = BoardManager.Instance.GetSquareTransform(end);

            if (movedPiece != null && targetSquare != null)
            {
                movedPiece.position = targetSquare.position;
                movedPiece.rotation = targetSquare.rotation;
            }
        }

    }

    public void SetPlayerRole(PlayerRole role)
    {
        if (IsServer)
        {
            playerRole.Value = role;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSpawnPositionServerRpc(Vector3 position, NetworkObjectReference parentRef)
    {
        transform.position = position;

        // Set the parent to the given one (usually the board square or similar)
        if (parentRef.TryGet(out NetworkObject parentObj))
        {
            Transform parentTransform = parentObj.transform;

            transform.SetParent(parentTransform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // Adjust the image position based on the player's side
            if (playerRenderer != null)
            {
                RectTransform rect = playerRenderer.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // Reset RectTransform properties before applying the offsets
                    rect.localPosition = Vector3.zero;
                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;

                    // Apply the offsets based on the player side
                    if (playerSide.Value == Side.White)
                    {
                        rect.anchoredPosition = new Vector2(125, 0); // Player 1 skin offset
                    }
                    else if (playerSide.Value == Side.Black)
                    {
                        rect.anchoredPosition = new Vector2(560, 600); // Player 2 skin offset 
                    }
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerSideServerRpc(Side side)
    {
        playerSide.Value = side;
    }
}

