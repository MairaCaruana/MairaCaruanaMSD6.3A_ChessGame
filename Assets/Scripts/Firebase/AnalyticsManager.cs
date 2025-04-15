using System;
using System.Collections.Generic;
using UnityEngine.Analytics;

public static class AnalyticsManager
{
    public static void LogMatchStart(string playerId)
    {
        Analytics.CustomEvent("match_start", new Dictionary<string, object>
        {
            { "player_id", playerId },
            { "match_id", Guid.NewGuid().ToString() },
            { "timestamp", DateTime.UtcNow.ToString("o") }
        });
    }

    public static void LogMatchEnd(string winnerId, float duration, string openingMove)
    {
        Analytics.CustomEvent("match_end", new Dictionary<string, object>
        {
            { "winner_id", winnerId },
            { "duration", duration },
            { "timestamp", DateTime.UtcNow.ToString("o") },
            { "opening_move", openingMove }
        });
    }

    public static void LogDLCPurchase(string itemName, int price, string playerId)
    {
        Analytics.CustomEvent("dlc_purchase", new Dictionary<string, object>
        {
            { "item_name", itemName },
            { "price", price },
            { "player_id", playerId }
        });
    }
}
