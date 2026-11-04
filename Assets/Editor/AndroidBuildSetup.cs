using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class AndroidBuildSetup
{
    static AndroidBuildSetup()
    {
        EditorApplication.delayCall += ApplyAndroidSettings;
    }

    [MenuItem("Tools/Vaelstrider/Apply Android Settings & Icons")]
    public static void ApplyAndroidSettings()
    {
        try
        {
            // 1. Force Landscape Orientation
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // 2. Set Graphics APIs for Android: OpenGLES3 ONLY (eliminates Vulkan crashes on mobile)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[] {
                GraphicsDeviceType.OpenGLES3
            });

            // 3. Setup Custom App Icon & Disable Splash
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/AppIcon.png");
            if (icon != null)
            {
                // Default global icon & Android icon
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new Texture2D[] { icon }, IconKind.Any);
                PlayerSettings.SetIcons(NamedBuildTarget.Android, new Texture2D[] { icon }, IconKind.Any);
                Debug.Log("<color=green>[AndroidBuildSetup]</color> Đã nạp thành công App Icon mới cho Android!");
            }
            else
            {
                Debug.LogWarning("[AndroidBuildSetup] Chưa tìm thấy file Assets/Sprites/AppIcon.png!");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[AndroidBuildSetup]</color> Cấu hình Android hoàn tất: LandscapeLeft cố định, OpenGLES3 ưu tiên, Icon tùy chỉnh!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AndroidBuildSetup] Lỗi thiết lập Android: {ex.Message}");
        }
    }

    [MenuItem("Tools/Vaelstrider/Build Android APK")]
    public static void BuildAPK()
    {
        ApplyAndroidSettings();
        PlayerSettings.bundleVersion = "1.3";
        PlayerSettings.Android.bundleVersionCode = 4;

        string[] scenes = new string[] {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/Map1_Thestart.unity",
            "Assets/Scenes/Map2_ValleyRuin.unity",
            "Assets/Scenes/Map3_ForgottenMemories.unity"
        };

        string outputApk = "Vaelstrider_v5.apk";
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = outputApk;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"<color=green>[BUILD SUCCESS]</color> APK đã xuất thành công tại: {outputApk} ({report.summary.totalSize / (1024 * 1024)} MB)");
        }
        else
        {
            Debug.LogError($"<color=red>[BUILD FAILED]</color> Lỗi xuất APK: {report.summary.result}");
        }
    }
}
