using UnityEngine;
using Unity.Netcode;
using UnityChess;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Initialize network-based game logic
        StartNetworkGame();
    }

    private void StartNetworkGame()
    {
        // Ensure the main game logic is started
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    [ServerRpc]
    public void ExecuteMoveServerRpc(int startX, int startY, int endX, int endY)
    {
        if (GameManager.Instance != null)
        {
            Square startSquare = new Square(startX, startY);
            Square endSquare = new Square(endX, endY);

            if (GameManager.Instance.game.TryGetLegalMove(startSquare, endSquare, out Movement move))
            {
                if (GameManager.Instance.TryExecuteMove(move))
                {
                    // Broadcast the move to all clients
                    ExecuteMoveClientRpc(startX, startY, endX, endY);
                }
            }
        }
    }

    [ClientRpc]
    private void ExecuteMoveClientRpc(int startX, int startY, int endX, int endY)
    {
        // Update the move on all clients
        Debug.Log($"Move executed from ({startX}, {startY}) to ({endX}, {endY})");
    }
}
