using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Unity.Netcode;

[System.Serializable]
public class GameState
{
    public int turnNumber;
    public string[,] boardState; 
    public string whitePlayerId;
    public string blackPlayerId;
    public string currentTurnPlayerId;
    public string whiteSkin;
    public string blackSkin;
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void SaveGameState(string boardString, int turnNumber, string currentTurnPlayerId, string whitePlayerId, string blackPlayerId, string whiteSkin, string blackSkin)
    {
        var gameState = new Dictionary<string, object>
        {
            { "turnNumber", turnNumber },
            { "currentTurnPlayerId", currentTurnPlayerId },
            { "whitePlayerId", whitePlayerId },
            { "blackPlayerId", blackPlayerId },
            { "whiteSkin", whiteSkin },
            { "blackSkin", blackSkin },
            { "board", boardString },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        };

        db.Collection("game_states").Document("latest_match").SetAsync(gameState).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Debug.Log("✅ Game state saved.");
            else
                Debug.LogError("❌ Save failed: " + task.Exception);
        });
    }

    public void LoadLatestGameState()
    {
        db.Collection("game_states").Document("latest_match").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var doc = task.Result;
                string boardString = doc.GetValue<string>("board");
                string currentTurn = doc.GetValue<string>("currentTurnPlayerId");
                string whiteSkin = doc.GetValue<string>("whiteSkin");
                string blackSkin = doc.GetValue<string>("blackSkin");

                //Apply the restored values 
               BoardManager.Instance.ApplyBoardFromString(boardString);
               NetworkPlayer.Instance.ApplySkin(whiteSkin);
               NetworkPlayer.Instance.ApplySkin(blackSkin);
                // Sync across clients
                SyncToClientsClientRpc(boardString, currentTurn, whiteSkin, blackSkin);
            }
            else
            {
                Debug.LogError("Failed to load: " + task.Exception);
            }
        });
    }

    [ClientRpc]
    private void SyncToClientsClientRpc(string boardString, string currentTurnPlayerId, string whiteSkin, string blackSkin)
    {
        BoardManager.Instance.ApplyBoardFromString(boardString);
        NetworkPlayer.Instance.ApplySkin(whiteSkin);
        NetworkPlayer.Instance.ApplySkin(blackSkin);
        // You can store currentTurnPlayerId where needed
    }
}
