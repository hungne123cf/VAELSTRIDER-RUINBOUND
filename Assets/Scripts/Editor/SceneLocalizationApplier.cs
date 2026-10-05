using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class SceneLocalizationApplier
{
    private static readonly string[] ALL_SCENES = new string[]
    {
        "Assets/Scenes/LoginScene.unity",
        "Assets/Scenes/MainMenuScene.unity",
        "Assets/Scenes/Map1_Thestart.unity",
        "Assets/Scenes/Map2_ValleyRuin.unity",
        "Assets/Scenes/Map3_ForgottenMemories.unity"
    };

    static SceneLocalizationApplier()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Scan and apply once if trigger or needed
        };
    }

    [MenuItem("Vaelstrider/Localization/Quét & Chuyển Đổi Toàn Bộ Scene Sang Đa Ngôn Ngữ Linh Hoạt", false, 50)]
    public static void ApplyLocalizationToAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[LOCALIZATION] Không thể thực hiện khi đang Play Mode!");
            return;
        }

        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        int totalTextsProcessed = 0;

        foreach (string scenePath in ALL_SCENES)
        {
            if (!File.Exists(scenePath)) continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int countInScene = ProcessSceneTexts();
            totalTextsProcessed += countInScene;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=cyan>[LOCALIZATION]</color> Đã xử lý {countInScene} UI Text trong scene: {scenePath}");
        }

        // Reopen original scene
        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"<color=green>[LOCALIZATION SUCCESS]</color> Đã chuyển đổi thành công {totalTextsProcessed} text trong toàn bộ {ALL_SCENES.Length} scene sang hệ thống đa ngôn ngữ linh hoạt!");
    }

    public static int ProcessSceneTexts()
    {
        int count = 0;

        // 1. TextMeshProUGUI
        var allTmp = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tmp in allTmp)
        {
            if (tmp == null || tmp.gameObject == null) continue;
            ApplyToTMP(tmp);
            count++;
        }

        // 2. Legacy UI Text
        var allLegacy = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var txt in allLegacy)
        {
            if (txt == null || txt.gameObject == null) continue;
            ApplyToLegacyText(txt);
            count++;
        }

        return count;
    }

    private static void ApplyToTMP(TextMeshProUGUI tmp)
    {
        var loc = tmp.GetComponent<LocalizedText>();
        if (loc == null)
        {
            loc = tmp.gameObject.AddComponent<LocalizedText>();
        }

        string currentText = tmp.text != null ? tmp.text.Trim() : "";
        string objName = tmp.gameObject.name.ToLower();

        string matchedKey = MatchKey(currentText, objName);
        if (!string.IsNullOrEmpty(matchedKey))
        {
            loc.Key = matchedKey;
            loc.Refresh();
            return;
        }

        // Nếu không khớp từ điển cố định, tự động trích xuất cặp tiếng Việt - Anh
        var autoPair = GenerateBilingualPair(currentText, objName);
        if (!string.IsNullOrEmpty(autoPair.vi) && !string.IsNullOrEmpty(autoPair.en))
        {
            loc.SetTexts(autoPair.vi, autoPair.en);
            loc.Refresh();
        }
    }

    private static void ApplyToLegacyText(Text txt)
    {
        var loc = txt.GetComponent<LocalizedText>();
        if (loc == null)
        {
            loc = txt.gameObject.AddComponent<LocalizedText>();
        }

        string currentText = txt.text != null ? txt.text.Trim() : "";
        string objName = txt.gameObject.name.ToLower();

        string matchedKey = MatchKey(currentText, objName);
        if (!string.IsNullOrEmpty(matchedKey))
        {
            loc.Key = matchedKey;
            loc.Refresh();
            return;
        }

        var autoPair = GenerateBilingualPair(currentText, objName);
        if (!string.IsNullOrEmpty(autoPair.vi) && !string.IsNullOrEmpty(autoPair.en))
        {
            loc.SetTexts(autoPair.vi, autoPair.en);
            loc.Refresh();
        }
    }

    private static string MatchKey(string rawText, string objName)
    {
        string t = rawText.ToUpper();
        string o = objName.ToUpper();

        // Title & App
        if (t.Contains("VAELSTRIDER") || o.Contains("APPTITLE") || o.Contains("TITLETEXT")) return "APP_TITLE";
        if (t.Contains("HÀNH TRÌNH TÀN TÍCH") || t.Contains("JOURNEY TO THE ANCIENT")) return "LOGIN_SUBTITLE";
        if (t.Contains("PHIÊN BẢN") || t.Contains("VERSION V1")) return "LOGIN_VERSION";

        // Navigation & Action Buttons
        if (t == "XUẤT TRẬN" || t == "PLAY" || t == "BATTLE" || t == "BẮT ĐẦU" || o.Contains("BTN_PLAY")) return "BTN_PLAY";
        if (t == "NHÂN VẬT" || t == "HEROES" || t == "ROSTER" || o.Contains("BTN_ROSTER")) return "BTN_ROSTER";
        if (t == "CỬA HÀNG" || t == "SHOP" || o.Contains("BTN_SHOP")) return "BTN_SHOP";
        if (t == "CÀI ĐẶT" || t == "SETTINGS" || o.Contains("BTN_SETTINGS")) return "BTN_SETTINGS";
        if (t == "ĐĂNG XUẤT" || t == "LOGOUT" || o.Contains("BTN_LOGOUT")) return "BTN_LOGOUT";
        if (t == "THOÁT" || t == "EXIT" || o.Contains("BTN_EXIT")) return "BTN_EXIT";
        if (t == "QUAY LẠI" || t == "BACK" || o.Contains("BTN_BACK")) return "BTN_BACK";
        if (t == "CHỌN BẢN ĐỒ" || t == "SELECT MAP" || o.Contains("BTN_MAPSELECT")) return "BTN_MAP_SELECT";
        if (t == "XÁC NHẬN" || t == "CONFIRM" || o.Contains("CONFIRM")) return "BTN_CONFIRM";
        if (t == "HỦY" || t == "CANCEL" || o.Contains("CANCEL")) return "BTN_CANCEL";
        if (t == "CHƠI LẠI" || t == "RESTART" || t == "PLAY AGAIN") return "BTN_RESTART";
        if (t == "TIẾP TỤC" || t == "RESUME") return "BTN_RESUME";
        if (t == "TRANG CHỦ" || t == "MAIN MENU") return "BTN_MAIN_MENU";
        if (t == "HƯỚNG DẪN" || t == "HOW TO PLAY") return "BTN_HOW_TO_PLAY";
        if (t == "XÓA DỮ LIỆU" || t == "RESET DATA") return "BTN_RESET_DATA";

        // Top info
        if (t == "RUIN WANDERER" || t == "LỮ KHÁCH TÀN TÍCH") return "LBL_PLAYER_TITLE";
        if (t.StartsWith("SHARDS:") || t.StartsWith("LINH HỒN:")) return "LBL_SHARDS";
        if (t.StartsWith("POTIONS:") || t.StartsWith("BÌNH MÁU:")) return "LBL_POTIONS";

        // Characters
        if (t.Contains("HIỆP SĨ CÂN BẰNG") || t.Contains("VAEL -")) return "HERO_VAEL_NAME";
        if (t.Contains("HẦU GÁI SÁT THỦ") || t.Contains("ELYSIA -")) return "HERO_MAID_NAME";

        // Maps
        if (t.Contains("TÀN TÍCH THUNG LŨNG") || t.Contains("VALLEY RUINS")) return "MAP1_NAME";
        if (t.Contains("NGỤC TỐI RỰC LỬA") || t.Contains("FLAME DUNGEON")) return "MAP2_NAME";
        if (t.Contains("KÝ ỨC LÃNG QUÊN") || t.Contains("FORGOTTEN MEMORIES")) return "MAP3_NAME";

        // Prompts
        if (t.Contains("TẾ ĐÀN CỔ ĐẠI") || t.Contains("ANCIENT SHRINE")) return "PROMPT_SHRINE";
        if (t.Contains("MỞ RƯƠNG") || t.Contains("OPEN TREASURE")) return "PROMPT_CHEST";
        if (t.Contains("CỔNG KHÔNG GIAN") || t.Contains("RUIN PORTAL")) return "PROMPT_PORTAL";

        return null;
    }

    private static (string vi, string en) GenerateBilingualPair(string rawText, string objName)
    {
        if (string.IsNullOrEmpty(rawText)) return ("", "");

        string lower = rawText.ToLower();

        if (lower.Contains("hp:") || lower.Contains("atk:") || lower.Contains("spd:"))
        {
            return (rawText, rawText);
        }
        if (lower.Contains("tuyệt kỹ:") || lower.Contains("ultimate:"))
        {
            return ("TUYỆT KỸ: ...", "ULTIMATE: ...");
        }
        if (lower.Contains("chiến thắng") || lower.Contains("victory"))
        {
            return ("CHIẾN THẮNG!", "VICTORY!");
        }
        if (lower.Contains("tử trận") || lower.Contains("you died") || lower.Contains("game over"))
        {
            return ("BẠN ĐÃ TỬ TRẬN", "YOU DIED");
        }

        // Mặc định giữ text gốc cho cả 2 nếu là ký tự biểu tượng hoặc số
        return (rawText, rawText);
    }
}
