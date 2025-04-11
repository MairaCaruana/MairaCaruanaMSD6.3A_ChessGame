using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class GameStateManager : MonoBehaviour
{
    FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void SaveGameState(string boardState, string turn)
    {
        string playerId = PlayerPrefs.GetString("user_id", "guest");

        Dictionary<string, object> gameState = new Dictionary<string, object> {
            { "player_id", playerId },
            { "board_state", boardState },
            { "turn", turn },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        };

        db.Collection("game_states").AddAsync(gameState).ContinueWithOnMainThread(task => {
            Debug.Log("?? Game state saved.");
        });
    }

    public void LoadLatestGameState()
    {
        string playerId = PlayerPrefs.GetString("user_id", "guest");

        db.Collection("game_states")
          .WhereEqualTo("player_id", playerId)
          .OrderBy("timestamp")
          .Limit(1)
          .GetSnapshotAsync().ContinueWithOnMainThread(task => {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  foreach (var doc in task.Result.Documents)
                  {
                      string board = doc.GetValue<string>("board_state");
                      string turn = doc.GetValue<string>("turn");

                      Debug.Log($"?? Restored: {board}, Turn: {turn}");
                      // TODO: Apply this state to your board
                  }
              }
              else
              {
                  Debug.LogError("? Failed to load game state: " + task.Exception);
              }
          });
    }
}
