using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BuildScenesAutoRegistrar
{
    public static readonly string[] RequiredScenes = new string[]
    {
        "Assets/Scenes/LoginScene.unity",
        "Assets/Scenes/MainMenuScene.unity",
        "Assets/Scenes/Map1_Thestart.unity",
        "Assets/Scenes/Map2_ValleyRuin.unity",
        "Assets/Scenes/Map3_ForgottenMemories.unity"
    };

    static BuildScenesAutoRegistrar()
    {
        EditorApplication.delayCall += EnsureBuildScenes;
    }

    [MenuItem("Vaelstrider/Scenes/Cập Nhật Build Settings (5 Scenes Chuẩn)", false, 10)]
    public static void ForceSyncMenu()
    {
        EnsureBuildScenes(true);
    }

    public static void EnsureBuildScenes()
    {
        EnsureBuildScenes(false);
    }

    public static void EnsureBuildScenes(bool forceLog)
    {
        var currentScenes = EditorBuildSettings.scenes;
        bool needsUpdate = false;

        if (currentScenes == null || currentScenes.Length != RequiredScenes.Length)
        {
            needsUpdate = true;
        }
        else
        {
            for (int i = 0; i < RequiredScenes.Length; i++)
            {
                if (currentScenes[i].path != RequiredScenes[i] || !currentScenes[i].enabled)
                {
                    needsUpdate = true;
                    break;
                }
            }
        }

        if (needsUpdate)
        {
            List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene>();
            foreach (var scenePath in RequiredScenes)
            {
                if (File.Exists(scenePath))
                {
                    scenesList.Add(new EditorBuildSettingsScene(scenePath, true));
                }
                else
                {
                    Debug.LogWarning($"[BuildScenesAutoRegistrar] Scene path not found on disk: {scenePath}");
                }
            }

            EditorBuildSettings.scenes = scenesList.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>[BuildScenesAutoRegistrar]</color> Đã tự động đồng bộ {scenesList.Count} Scenes vào Build Settings thành công!");
        }
        else if (forceLog)
        {
            Debug.Log("<color=green>[BuildScenesAutoRegistrar]</color> Build Settings đã chuẩn đầy đủ 5 scenes.");
        }
    }
}
