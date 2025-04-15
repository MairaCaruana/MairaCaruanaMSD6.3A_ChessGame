using System.IO;
using Unity.Collections;
using Unity.Netcode;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;

public class NetworkPlayer : NetworkBehaviour
 {
    public static NetworkPlayer Instance { get; private set; }

    private NetworkObject netObj;

    public NetworkVariable<FixedString64Bytes> currentSkin = new NetworkVariable<FixedString64Bytes>();
    public Image playerRenderer;

    public NetworkVariable<PlayerRole> playerRole = new NetworkVariable<PlayerRole>(PlayerRole.None);

    public Side PlayerSide => playerSide.Value;

    private NetworkVariable<Side> playerSide = new NetworkVariable<Side>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentSkin.OnValueChanged += OnSkinChanged;

        // Apply the current skin when the player joins or late-spawns
        ApplySkin(currentSkin.Value.ToString());

        if (IsOwner)
        {
            // Only set the instance for the local player's NetworkPlayer
            Instance = this;
            Debug.Log($"[Client {OwnerClientId}] NetworkPlayer spawned and set as Instance. My side is: {playerSide.Value}");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentSkin.OnValueChanged -= OnSkinChanged;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSkinChanged(FixedString64Bytes oldSkin, FixedString64Bytes newSkin)
    {
        ApplySkin(newSkin.ToString());
    }


    private void Awake()
    {
        netObj = GetComponent<NetworkObject>();
    }

    public void ApplySkin(string skinName)
    {
        if (SkinManager.Instance == null)
        {
            Debug.LogError("SkinManager is null!");
            return;
        }

        bool isSelf = IsOwner || (IsServer && IsHost); 

        if (isSelf)
        {
            // Only check for ownership if this is the local player applying the skin
            if (!SkinManager.Instance.HasSkin(skinName))
            {
                Debug.LogWarning($"Skin {skinName} has not been purchased yet!");
                return;
            }

            Debug.Log($"Player {OwnerClientId} applied skin: {skinName}");

            // Sync with others
            ApplySkinServerRpc(skinName);
        }

        // Regardless of ownership, try to update the player visually
        UpdatePlayerSkin(skinName);
    }

    [ClientRpc]
    private void ForceApplySkinClientRpc(string skinName)
    {
        // This will run on all clients (including host), and re-apply the skin visually
        UpdatePlayerSkin(skinName);
    }


    [ServerRpc]
    private void ApplySkinServerRpc(string skinName)
    {
        currentSkin.Value = skinName; // triggers OnSkinChanged for most clients
        ForceApplySkinClientRpc(skinName); // ensures all clients (including host) get it visually
    }


    private async void UpdatePlayerSkin(string skinName)
    {
        string skinPathJpg = Path.Combine(Application.persistentDataPath, skinName + ".jpg");
        string skinPathJpeg = Path.Combine(Application.persistentDataPath, skinName + ".jpeg");
        string localPath = null;

        // Check if the file exists with its original extension
        // Check if the file exists locally
        if (File.Exists(skinPathJpg))
        {
            localPath = skinPathJpg;
        }
        else if (File.Exists(skinPathJpeg))
        {
            localPath = skinPathJpeg;
        }
        else
        {
            Debug.Log($"Skin {skinName} not found locally. Attempting to download from Firebase...");

            try
            {
                // Attempt to download the skin (assumed to be .jpg)
                string downloadedPath = await DownloadSkinFromFirebaseAsync(skinName + ".jpg");

                if (File.Exists(downloadedPath))
                {
                    localPath = downloadedPath;
                    Debug.Log($"Skin {skinName} successfully downloaded from Firebase.");
                }
                else
                {
                    Debug.LogError($"Failed to download skin: {skinName}");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error downloading skin {skinName}: {ex.Message}");
                return;
            }
        }

        byte[] fileData = File.ReadAllBytes(localPath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            Debug.Log($"Skin {skinName} loaded and applied.");
            playerRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError($"Failed to create texture from skin: {skinName}");
        }
    }

    private async System.Threading.Tasks.Task<string> DownloadSkinFromFirebaseAsync(string fileName)
    {
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference skinRef = storage.GetReferenceFromUrl("gs://connectedgamingproject.firebasestorage.app").Child("Skins").Child(fileName);

        string localPath = Path.Combine(Application.persistentDataPath, fileName);
        await skinRef.GetFileAsync(localPath);
        return localPath;
    }


    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(int startFile, int startRank, int endFile, int endRank, ElectedPiece promotionType,
   ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[Server] Move requested from ({startFile},{startRank}) to ({endFile},{endRank})");

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

            SetVisualOffsetClientRpc(playerSide.Value);
        }
    }

    [ClientRpc]
    private void SetVisualOffsetClientRpc(Side side)
    {
        if (playerRenderer == null) return;

        RectTransform rect = playerRenderer.GetComponent<RectTransform>();
        if (rect == null) return;

        // Reset values
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        // Apply offset based on side
        if (side == Side.White)
        {
            rect.anchoredPosition = new Vector2(125, 0); // Bottom left-ish
        }
        else if (side == Side.Black)
        {
            rect.anchoredPosition = new Vector2(560, 600); // Top right-ish
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerSideServerRpc(Side side)
    {
        playerSide.Value = side;
    }
}

