using UnityEngine;

public static class LocalizationManager
{
    public enum Language
    {
        Vietnamese,
        English
    }

    public static Language CurrentLanguage
    {
        get => (Language)PlayerPrefs.GetInt("Game_Language", (int)Language.Vietnamese);
        set
        {
            PlayerPrefs.SetInt("Game_Language", (int)value);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }

    public static System.Action OnLanguageChanged;

    public static string Get(string key)
    {
        bool isVi = CurrentLanguage == Language.Vietnamese;
        switch (key)
        {
            // Settings UI
            case "SETTINGS_TITLE": return isVi ? "CÀI ĐẶT HỆ THỐNG" : "SYSTEM SETTINGS";
            case "AUDIO_BGM": return isVi ? "Nhạc Nền (BGM)" : "Music (BGM)";
            case "AUDIO_SFX": return isVi ? "Hiệu Ứng (SFX)" : "Sound Effects";
            case "LANGUAGE": return isVi ? "Ngôn Ngữ" : "Language";
            case "BTN_RESUME": return isVi ? "TIẾP TỤC" : "RESUME";
            case "BTN_RESTART": return isVi ? "CHƠI LẠI TỪ ĐẦU" : "RESTART RUN";
            case "BTN_GUIDE": return isVi ? "HƯỚNG DẪN CHƠI" : "HOW TO PLAY";
            case "BTN_CLOSE": return isVi ? "ĐÓNG" : "CLOSE";
            case "BTN_EXIT_LOGIN": return isVi ? "VỀ TRANG ĐĂNG NHẬP" : "EXIT TO LOGIN";

            // Game Over Screen
            case "GAMEOVER_TITLE": return isVi ? "BẠN ĐÃ GỤC NGÃ" : "YOU DIED";
            case "GAMEOVER_SUBTITLE": return isVi ? "Chuyến hành trình của bạn đã tạm dừng..." : "Your journey in the ruins has halted.";
            case "GAMEOVER_RESPAWN_AVAILABLE": return isVi ? "HỒI SINH TẠI CHỖ (1/1)" : "RESPAWN HERE (1/1)";
            case "GAMEOVER_RESPAWN_USED": return isVi ? "ĐÃ HẾT LƯỢT HỒI SINH (0/1)" : "OUT OF RESPAWNS (0/1)";
            case "GAMEOVER_RESTART_MAP": return isVi ? "CHƠI LẠI TỪ ĐẦU" : "RESTART MAP";
            case "GAMEOVER_EXIT_LOGIN": return isVi ? "VỀ TRANG ĐĂNG NHẬP" : "EXIT TO LOGIN";

            // Main Menu Hub
            case "MENU_TITLE": return isVi ? "SẢNH CHÍNH" : "MAIN HUB";
            case "BTN_PLAY": return isVi ? "XUẤT TRẬN" : "EXPEDITION";
            case "BTN_HERO": return isVi ? "NHÂN VẬT" : "HEROES";
            case "BTN_SHOP": return isVi ? "CỬA HÀNG" : "SHOP";
            case "BTN_SETTINGS": return isVi ? "CÀI ĐẶT" : "SETTINGS";
            case "BTN_LOGOUT": return isVi ? "ĐĂNG XUẤT" : "LOGOUT";

            // Map Selection
            case "MAP_SELECT_TITLE": return isVi ? "CHỌN KHU VỰC XUẤT TRẬN" : "SELECT EXPEDITION";
            case "MAP1_NAME": return isVi ? "HẦM NGỤC CỔ" : "DUNGEON OF OLD";
            case "MAP1_DESC": return isVi ? "Phế tích ngầm u tối với quái vật máy cổ đại và bẫy gai nguy hiểm." : "Dark underground crypt crawling with ancient machines.";
            case "MAP2_NAME": return isVi ? "THUNG LŨNG CỔ THỤ" : "VALLEY OF ANCIENT TREES";
            case "MAP2_DESC": return isVi ? "Vùng thung lũng thiên nhiên rộng mở với bóng mát Cổ Thụ nghìn năm." : "Vast open valley shaded by colossal millenary trees.";

            // Shop & Roster
            case "SHOP_TITLE": return isVi ? "CỬA HÀNG CỔ ĐẠI" : "ANCIENT SHOP";
            case "BUY_MAID": return isVi ? "MUA MAID CHIẾN BINH" : "BUY BATTLE MAID";
            case "BUY_POTION": return isVi ? "BÌNH MÁU HỒI PHỤC (+50 HP)" : "HEALTH POTION (+50 HP)";
            case "ROSTER_TITLE": return isVi ? "DANH SÁCH ANH HÙNG" : "HERO ROSTER";
            case "SELECT_HERO": return isVi ? "XUẤT TRẬN" : "SELECT";
            case "SELECTED": return isVi ? "ĐANG CHỌN" : "SELECTED";
            case "LOCKED": return isVi ? "CHƯA SỞ HỮU" : "LOCKED";
            case "SHARDS_NOT_ENOUGH": return isVi ? "Không đủ Tinh Thể Aethel!" : "Not enough Aethel Shards!";

            default: return key;
        }
    }
}
