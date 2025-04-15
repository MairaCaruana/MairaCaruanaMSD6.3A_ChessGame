using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    public static SkinManager Instance;

    public List<string> ownedSkins = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadOwnedSkins()
    {
        string ownedSkinsString = PlayerPrefs.GetString("OwnedSkins", "");
        ownedSkins = ownedSkinsString.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public bool HasSkin(string skinName)
    {
        return ownedSkins.Contains(skinName);
    }

    public void RegisterSkin(string skinName)
    {
        string baseName = Path.GetFileNameWithoutExtension(skinName); 
        if (!ownedSkins.Contains(baseName))
        {
            ownedSkins.Add(baseName);
            Debug.Log($"Registered skin: {baseName}");
        }
    }
}
