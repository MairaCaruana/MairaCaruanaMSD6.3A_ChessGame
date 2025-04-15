using UnityEngine;

public class MatchManager : MonoBehaviour
{
    float matchStartTime;
    string openingMove = "";

    void Start()
    {
        string playerId = PlayerPrefs.GetString("user_id", "guest");
        AnalyticsManager.LogMatchStart(playerId);
        matchStartTime = Time.time;
    }

    public void OnPlayerMove(string move)
    {
        if (string.IsNullOrEmpty(openingMove)) openingMove = move;
    }

    public void EndMatch(string winnerId)
    {
        float duration = Time.time - matchStartTime;
        AnalyticsManager.LogMatchEnd(winnerId, duration, openingMove);
    }
}
