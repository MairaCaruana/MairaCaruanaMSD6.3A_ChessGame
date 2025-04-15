using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;

/// <summary>
/// Represents a visual chess piece in the game. This component handles user interaction,
/// such as dragging and dropping pieces, and determines the closest square on the board
/// where the piece should land. It also raises an event when a piece has been moved.
/// </summary>
public class VisualPiece : NetworkBehaviour
{
    // Delegate for handling the event when a visual piece has been moved.
    // Parameters: the initial square of the piece, its transform, the closest square's transform,
    // and an optional promotion piece.
    public delegate void VisualPieceMovedAction(int file, int rank, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);

    // Static event raised when a visual piece is moved.
    public static event VisualPieceMovedAction VisualPieceMoved;

    // The colour (side) of the piece (White or Black).
    public Side PieceColor;

    public Square CurrentSquare { get; private set; }

    public void SetCurrentSquare(Square square)
    {
        CurrentSquare = square;
    }
    // The radius used to detect nearby board squares for collision detection.
    private const float SquareCollisionRadius = 9f;

    // The camera used to view the board.
    private Camera boardCamera;
    // The screen-space position of the piece when it is first picked up.
    private Vector3 piecePositionSS;
    // A reference to the piece's SphereCollider (if required for collision handling).
    private SphereCollider pieceBoundingSphere;
    // A list to hold potential board square GameObjects that the piece might land on.
    private List<GameObject> potentialLandingSquares;
    // A cached reference to the transform of this piece.
    private Transform thisTransform;
    private bool hasRequestedOwnership = false;
   // private bool isDragging = false;

    private NetworkObject networkObject;

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    private bool IsLocalPlayersTurn()
    {
        if (NetworkManager.Singleton.LocalClient == null ||
    NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            Debug.LogError("LocalClient or PlayerObject is null!");
            return false;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();

            if (NetworkGameManager.Instance == null)
            {
                Debug.LogError("NetworkGameManager.Instance is null!");
                return false;
            }

           // bool isLocalSideToMove = NetworkGameManager.Instance.NetworkSideToMove.Value == localPlayer.PlayerSide;
            bool isPieceOwnedByLocalPlayer = PieceColor == localPlayer.PlayerSide;

          //  Debug.Log($"[IsLocalPlayersTurn] isLocalSideToMove: {isLocalSideToMove}, isPieceOwnedByLocalPlayer: {isPieceOwnedByLocalPlayer}, sideToMove: {NetworkGameManager.Instance.NetworkSideToMove.Value}, localSide: {localPlayer.PlayerSide}");

            return //isLocalSideToMove &&
                isPieceOwnedByLocalPlayer;
        }

        Debug.LogWarning("[IsLocalPlayersTurn] Not a client or host.");
        return false;
    }


    /// <summary>
    /// Initialises the visual piece. Sets up necessary variables and obtains a reference to the main camera.
    /// </summary>
    private void Start()
    {
        // Initialise the list to hold potential landing squares.
        potentialLandingSquares = new List<GameObject>();
        // Cache the transform of this GameObject for efficiency.
        thisTransform = transform;
        // Obtain the main camera from the scene.
        boardCamera = Camera.main;
    }

    // Add these methods to your VisualPiece class
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to side-to-move changes     
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.NetworkSideToMove.OnValueChanged += OnSideToMoveChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Unsubscribe     
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.NetworkSideToMove.OnValueChanged -= OnSideToMoveChanged;
        }
    }

    private void OnSideToMoveChanged(Side oldSide, Side newSide)
    {
        // If it's this piece's turn, take ownership     
        if (PieceColor == newSide && NetworkObject != null)
        {
            // Only request ownership if we're the player who should control this piece         
            NetworkPlayer localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
            if (localPlayer.PlayerSide == newSide && !NetworkObject.IsOwner)
            {
                // Use the ServerRpc to request ownership
                RequestOwnershipServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"[RequestOwnershipServerRpc] Server received ownership request from Client {serverRpcParams.Receive.SenderClientId}");
        NetworkObject.ChangeOwnership(serverRpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Called when the user presses the mouse button over the piece.
    /// Records the initial screen-space position of the piece.
    /// </summary>
    public void OnMouseDown()
    {
        if (NetworkObject != null && !NetworkObject.IsOwner && !hasRequestedOwnership)
        {
            RequestOwnershipServerRpc();
            hasRequestedOwnership = true;
            Debug.Log("[OnMouseDown] Ownership requested via ServerRpc.");
            return; // Wait until we get ownership
        }

        if (!IsLocalPlayersTurn())
        {
            Debug.LogWarning($"[OnMouseDown] Not your turn or you don't own this piece. LocalSide={PieceColor}");
            return;
        }

        if (enabled)
        {
            Debug.Log($"[OnMouseDown] Enabled, trying to pick up piece. IsOwner: {NetworkObject.IsOwner}, hasRequestedOwnership: {hasRequestedOwnership}");

            // Only allow drag if we are the owner now
            if (NetworkObject.IsOwner)
            {
                piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);
              //  isDragging = true;
                Debug.Log("[OnMouseDown] Ownership confirmed, beginning drag.");
            }
            else
            {
                Debug.LogWarning("[OnMouseDown] Not owner, cannot drag.");
            }
        }
    }


    /// <summary>
    /// Called while the user drags the piece with the mouse.
    /// Updates the piece's world position to follow the mouse cursor.
    /// </summary>
    private void OnMouseDrag()
    {
        if (!IsLocalPlayersTurn())
        {
         return;
        }

        if (enabled)
        {
            // Create a new screen-space position based on the current mouse position
            Vector3 nextPiecePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
            // Convert to world space
            Vector3 newWorldPos = boardCamera.ScreenToWorldPoint(nextPiecePositionSS);

            if (NetworkObject != null && !NetworkObject.IsOwner)
            {
                Debug.LogWarning("[OnMouseDrag] Not owner during drag, requesting ownership again.");
                RequestOwnershipServerRpc();
                return;
            }

            thisTransform.position = newWorldPos;
        }
    }

    /// <summary>
    /// Called when the user releases the mouse button after dragging the piece.
    /// Determines the closest board square to the piece and raises an event with the move.
    /// </summary>
    public void OnMouseUp()
    {
        if (!enabled) return;

        //isDragging = false;
        hasRequestedOwnership = false;

        Debug.Log("[OnMouseUp] Drag released, calculating closest square...");

        // Clear any previous potential landing square candidates.
            potentialLandingSquares.Clear();
                // Obtain all square GameObjects within the collision radius of the piece's current position.
                BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);

                // If no squares are found, assume the piece was moved off the board and reset its position.
                if (potentialLandingSquares.Count == 0)
                { // piece moved off board
            Debug.LogWarning("[OnMouseUp] No valid landing squares found, resetting position.");
            thisTransform.position = BoardManager.Instance.GetSquareGOByPosition(CurrentSquare).transform.position;
                    return;
                }

                // Determine the closest square from the list of potential landing squares.
                Transform closestSquareTransform = potentialLandingSquares[0].transform;
                // Calculate the square of the distance between the piece and the first candidate square.
                float shortestDistanceFromPieceSquared = (closestSquareTransform.position - thisTransform.position).sqrMagnitude;

                // Iterate through remaining potential squares to find the closest one.
                for (int i = 1; i < potentialLandingSquares.Count; i++)
                {
                    Transform potentialLandingSquare = potentialLandingSquares[i].transform;
                    // Calculate the squared distance from the piece to the candidate square.
                    float distanceFromPieceSquared = (potentialLandingSquare.transform.position - thisTransform.position).sqrMagnitude;

                    // If the current candidate is closer than the previous closest, update the closest square.
                    if (distanceFromPieceSquared < shortestDistanceFromPieceSquared)
                    {
                        shortestDistanceFromPieceSquared = distanceFromPieceSquared;
                        closestSquareTransform = potentialLandingSquare;
                    }
                }

                // Find the closest board square to the current position
                GameObject startSquareGO = FindClosestSquare(thisTransform.position);
                if (startSquareGO != null)
                {
                    Square startSquare = new Square(startSquareGO.name);
            Debug.Log($"[OnMouseUp] Moved from closest square: {startSquare.File},{startSquare.Rank}");
            VisualPieceMoved?.Invoke(startSquare.File, startSquare.Rank, thisTransform, closestSquareTransform);
                   
                }
                }
        
        

    // Helper method to find the closest square to a position
    private GameObject FindClosestSquare(Vector3 position)
    {
        GameObject[] squares = GameObject.FindGameObjectsWithTag("Square");
        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject square in squares)
        {
            float distance = (square.transform.position - position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = square;
            }
        }

        return closest;
    }
}
