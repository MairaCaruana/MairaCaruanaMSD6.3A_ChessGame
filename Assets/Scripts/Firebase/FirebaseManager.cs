using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static bool IsFirebaseReady = false;

    async void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            IsFirebaseReady = true;
            Debug.Log("Firebase Initialized");
        });

        // Unity Services (Analytics)
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services Initialized");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unity Services Init Failed: " + e.Message);
        }

        if (!PlayerPrefs.HasKey("user_id"))
        {
            PlayerPrefs.SetString("user_id", System.Guid.NewGuid().ToString());
            Debug.Log("Generated new user ID: " + PlayerPrefs.GetString("user_id"));
        }

    }
}
