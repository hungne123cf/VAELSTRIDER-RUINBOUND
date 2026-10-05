using System;
using System.IO;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class ShardsFixTool
{
    static ShardsFixTool()
    {
        EditorApplication.delayCall += ExecuteFix;
    }

    [MenuItem("Tools/Vaelstrider/Restore 8B Shards")]
    public static void ExecuteFix()
    {
        try
        {
            string dbPath = Path.Combine(Application.persistentDataPath, "vaelstrider_accounts.json");
            if (File.Exists(dbPath))
            {
                string json = File.ReadAllText(dbPath);
                if (json.Contains("\"aethelShards\": 2147282406") || json.Contains("\"aethelShards\":2147282406") || json.Contains("2147483647"))
                {
                    json = System.Text.RegularExpressions.Regex.Replace(json, @"""aethelShards""\s*:\s*2147\d+", "\"aethelShards\": 8000000000");
                    File.WriteAllText(dbPath, json);
                    Debug.Log("<color=green>[SHARDS FIX]</color> Đã khôi phục thành công 8,000,000,000 Aethel Shards trong vaelstrider_accounts.json!");
                }
            }

            PlayerPrefs.SetString("Save_AethelShards_Long", "8000000000");
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SHARDS FIX] {ex.Message}");
        }
    }
}
