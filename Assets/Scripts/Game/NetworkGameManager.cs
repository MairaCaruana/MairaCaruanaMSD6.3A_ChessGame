using UnityEngine;
using Unity.Netcode;
using UnityChess;
public enum PlayerRole
{
    None,
    White,
    Black
}

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    public NetworkVariable<PlayerRole> HostPlayerRole = new NetworkVariable<PlayerRole>(PlayerRole.White);
    public NetworkVariable<PlayerRole> ClientPlayerRole = new NetworkVariable<PlayerRole>(PlayerRole.Black);
    private bool rolesSpawned = false;
    [SerializeField] private Transform whitePlayerSpawn;
    [SerializeField] private Transform blackPlayerSpawn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected.");

        // Wait until both players are connected before assigning
        if (NetworkManager.Singleton.ConnectedClients.Count < 2) return;

        Debug.Log("Both players connected. Assigning roles & spawning positions...");

        bool whiteAssigned = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<NetworkPlayer>();
            if (player == null) continue;

            if (!whiteAssigned)
            {
                NetworkObject spawnPointNetObj = whitePlayerSpawn.GetComponent<NetworkObject>();
                whiteAssigned = true;
                player.SetPlayerRole(PlayerRole.White);
                player.SetPlayerSideServerRpc(Side.White);
                player.SetSpawnPositionServerRpc(whitePlayerSpawn.position, spawnPointNetObj);
            }
            else
            {
                NetworkObject spawnPointNetObj = blackPlayerSpawn.GetComponent<NetworkObject>();
                player.SetPlayerRole(PlayerRole.Black);
                player.SetPlayerSideServerRpc(Side.Black);
                player.SetSpawnPositionServerRpc(blackPlayerSpawn.position, spawnPointNetObj);
            }
        }

        GameManager.Instance.StartNewGame(true);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
        // Optional: Implement rejoin logic with session ID
    }


    [ClientRpc]
    public void NotifyGameEndClientRpc(string result)
    {
        Debug.Log("Game Over: " + result);
    }

    [ClientRpc]
    public void UpdateBoardInteractivityClientRpc(Side sideToEnable)
    {
        if (NetworkPlayer.Instance == null) return;

        if (NetworkPlayer.Instance.PlayerSide == sideToEnable)
        {
            BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(sideToEnable);
        }
    }


}
