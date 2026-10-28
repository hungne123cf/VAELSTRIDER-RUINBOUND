using UnityEngine;
using System;
using System.Collections.Generic;

public static class LocalizationManager
{
    public enum Language
    {
        Vietnamese = 0,
        English = 1
    }

    private const string PREF_LANG_KEY = "Game_Language_Preference";

    public static Action OnLanguageChanged;

    public static Language CurrentLanguage
    {
        get => (Language)PlayerPrefs.GetInt(PREF_LANG_KEY, (int)Language.Vietnamese);
        set
        {
            PlayerPrefs.SetInt(PREF_LANG_KEY, (int)value);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }
    }

    public static bool IsVietnamese => CurrentLanguage == Language.Vietnamese;

    public static string T(string vi, string en)
    {
        return CurrentLanguage == Language.Vietnamese ? vi : en;
    }

    public static void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == Language.Vietnamese ? Language.English : Language.Vietnamese;
    }

    public static void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
    }

    private static readonly Dictionary<string, (string vi, string en)> localizedStrings = new Dictionary<string, (string, string)>
    {
        // General UI & Login
        { "APP_TITLE", ("VAELSTRIDER: RUINBOUND", "VAELSTRIDER: RUINBOUND") },
        { "LOGIN_SUBTITLE", ("HÀNH TRÌNH TÀN TÍCH THƯỢNG CỔ", "JOURNEY TO THE ANCIENT RUINS") },
        { "BTN_GUEST_LOGIN", ("ĐĂNG NHẬP KHÁCH", "GUEST LOGIN") },
        { "BTN_GOOGLE_LOGIN", ("ĐĂNG NHẬP GOOGLE", "GOOGLE LOGIN") },
        { "LOGIN_VERSION", ("Phiên bản v1.0.0 - Sẵn sàng chiến đấu", "Version v1.0.0 - Ready for Battle") },

        // Top Bar & Account Info
        { "LBL_PLAYER_DEFAULT_NAME", ("CHIẾN BINH", "HERO") },
        { "LBL_PLAYER_TITLE", ("LỮ KHÁCH TÀN TÍCH", "RUIN WANDERER") },
        { "LBL_SHARDS", ("LINH HỒN", "SHARDS") },
        { "LBL_POTIONS", ("BÌNH MÁU", "POTIONS") },
        { "BTN_LOGOUT", ("ĐĂNG XUẤT", "LOGOUT") },

        // Navigation & Menu Buttons
        { "BTN_PLAY", ("XUẤT TRẬN", "BATTLE") },
        { "BTN_MAP_SELECT", ("CHỌN BẢN ĐỒ", "SELECT MAP") },
        { "BTN_ROSTER", ("NHÂN VẬT", "HEROES") },
        { "BTN_SKILLS", ("KỸ NĂNG", "SKILLS") },
        { "BTN_INVENTORY", ("TÚI ĐỒ", "BAG") },
        { "BTN_SHOP", ("CỬA HÀNG", "SHOP") },
        { "BTN_UPGRADE", ("NÂNG CẤP", "UPGRADES") },
        { "BTN_SETTINGS", ("CÀI ĐẶT", "SETTINGS") },
        { "BTN_EXIT", ("THOÁT", "EXIT") },
        { "BTN_BACK", ("QUAY LẠI", "BACK") },
        { "BTN_CONFIRM", ("XÁC NHẬN", "CONFIRM") },
        { "BTN_CANCEL", ("HỦY", "CANCEL") },
        { "BTN_RESET_DATA", ("XÓA DỮ LIỆU", "RESET DATA") },
        { "BTN_RESUME", ("TIẾP TỤC", "RESUME") },
        { "BTN_RESTART", ("CHƠI LẠI", "RESTART") },
        { "BTN_MAIN_MENU", ("TRANG CHỦ", "MAIN MENU") },
        { "BTN_HOW_TO_PLAY", ("HƯỚNG DẪN", "HOW TO PLAY") },
        { "BTN_SELECT", ("LỰA CHỌN", "SELECT") },
        { "BTN_SELECTED", ("ĐANG CHỌN", "SELECTED") },
        { "BTN_LOCKED", ("ĐÃ KHÓA", "LOCKED") },
        { "BTN_TEMP_LOCKED", ("TẠM KHÓA (SẮP RA MẮT)", "LOCKED (COMING SOON)") },
        { "BTN_UNLOCK", ("MỞ KHÓA", "UNLOCK") },

        // Audio & Settings
        { "SETTINGS_TITLE", ("CÀI ĐẶT HỆ THỐNG", "SETTINGS") },
        { "LBL_BGM", ("ÂM NHẠC (BGM)", "MUSIC VOLUME") },
        { "LBL_SFX", ("HIỆU ỨNG (SFX)", "SFX VOLUME") },
        { "LBL_LANGUAGE", ("NGÔN NGỮ", "LANGUAGE") },
        { "LANG_CURRENT", ("Tiếng Việt", "English") },
        { "MSG_RESET_CONFIRM", ("Bạn có chắc chắn muốn xóa toàn bộ dữ liệu lưu?", "Are you sure you want to reset all game data?") },
        { "MSG_RESET_SUCCESS", ("Đã đặt lại dữ liệu thành công!", "Game data reset successfully!") },

        // Maps
        { "MAP1_NAME", ("Tàn Tích Thung Lũng", "Valley Ruins") },
        { "MAP1_DESC", ("Vùng đất thung lũng đá cổ xưa, nơi ngự trị của Quái Thú và Thạch Thần.", "Ancient rocky valley inhabited by Valley Beasts and the Stone Golem.") },
        { "MAP2_NAME", ("Ngục Tối Rực Lửa", "Flame Dungeon") },
        { "MAP2_DESC", ("Hầm ngục rực lửa với những ngọn đuốc bí ẩn và Vua Xương Bất Diệt.", "Blazing dungeon lit by mystic torches and ruled by the Undying Skeleton King.") },
        { "MAP3_NAME", ("Ký Ức Lãng Quên", "Forgotten Memories") },
        { "MAP3_DESC", ("Cung điện thần thánh u ám, nơi chôn giấu sức mạnh tà ác của Huyết Quái.", "Desolate divine palace harboring the corrupt essence of the Blood Fiend.") },

        // Characters
        { "HERO_VAEL_NAME", ("Hiệp Sĩ Tiên Phong", "Holy Vanguard") },
        { "HERO_VAEL_ROLE", ("Đấu Sĩ / Thánh Kiếm Cổ Tộc", "Warrior / Holy Blade Master") },
        { "HERO_VAEL_SKILL", ("Tuyệt Kỹ: Cơn Lốc Tàn Tích", "Skill: Ruin Whirlwind") },
        { "HERO_VAEL_SKILL_DESC", ("Nhấn Giữ thủ Kiếm Thuẫn: Đỡ đòn hoàn hảo chặn 100% sát thương và phản kích x3.0 sát thương! Tuyệt kỹ bão lốc xoay kiếm hút quái nổ tung.", "Hold attack for Divine Parry: blocks 100% damage & retaliates with x3.0 Crit DMG! Special skill spins 360° storm pulling enemies.") },

        { "HERO_RONIN_NAME", ("Phong Lôi Lãng Khách", "Gale Ronin") },
        { "HERO_RONIN_ROLE", ("Sát Thủ / Phong Ấn & Ảo Ảnh", "Assassin / Phantom Blade") },
        { "HERO_RONIN_SKILL", ("Tuyệt Kỹ: Bão Phong Trảm", "Skill: Phantom Cleave") },
        { "HERO_RONIN_SKILL_DESC", ("Đòn chém quét 180° tích Phong Ấn bộc phá bạo kích & lốc xoáy. Tuyệt kỹ lướt ảo ảnh chém liên hoàn.", "180° cleave stacking Wind Marks for critical bursts & vortex. Special skill unleashes phantom dash slashes.") },

        { "HERO_MAID_NAME", ("Phong Lôi Lãng Khách", "Gale Ronin") },
        { "HERO_MAID_ROLE", ("Sát Thủ / Phong Ấn & Ảo Ảnh", "Assassin / Phantom Blade") },
        { "HERO_MAID_SKILL", ("Tuyệt Kỹ: Bão Phong Trảm", "Skill: Phantom Cleave") },
        { "HERO_MAID_SKILL_DESC", ("Đòn chém quét 180° tích Phong Ấn bộc phá bạo kích & lốc xoáy. Tuyệt kỹ lướt ảo ảnh chém liên hoàn.", "180° cleave stacking Wind Marks for critical bursts & vortex. Special skill unleashes phantom dash slashes.") },

        { "HERO_AZURE_NAME", ("Lam Kiếm Sĩ", "Azure Blade") },
        { "HERO_AZURE_ROLE", ("Xạ Thủ / Sóng Kiếm Băng Tầm Xa", "Marksman / Ranged Frost Waves") },
        { "HERO_AZURE_SKILL", ("Tuyệt Kỹ: Bão Sóng Kiếm", "Skill: Crescent Tempest") },
        { "HERO_AZURE_SKILL_DESC", ("Chém xuất sóng kiếm xuyên thấu quái. Tuyệt kỹ phóng 5 luồng đại kiếm khí nan quạt.", "Slashes unleash piercing waves. Special skill fires 5 massive crescent waves in a fan spread.") },

        { "HERO_MAGE_NAME", ("Pháp Sư Tinh Tú", "Celestial Mage") },
        { "HERO_MAGE_ROLE", ("Pháp Sư / Tinh Vân & Thiên Thạch", "Mage / Arcane & Meteor Tempest") },
        { "HERO_MAGE_SKILL", ("Tuyệt Kỹ: Thiên Thạch Ma Trận", "Skill: Meteor Apocalypse") },
        { "HERO_MAGE_SKILL_DESC", ("Phóng 3 cầu ma pháp tự định hướng bộc phá sét lan. Khiên ma trận hấp thụ sát thương và bão thiên thạch hủy diệt diện rộng.", "Fires 3 homing arcane orbs with chain lightning. Arcane barrier absorbs damage and summons a cataclysmic meteor storm.") },

        { "STAT_HP", ("MÁU", "HP") },
        { "STAT_ATK", ("TẤN CÔNG", "ATK") },
        { "STAT_SPD", ("TỐC ĐỘ", "SPD") },
        { "STAT_ULTI", ("TUYỆT KỸ", "ULTIMATE") },

        // Combat & HUD
        { "HUD_HEALTH", ("MÁU", "HP") },
        { "HUD_ENERGY", ("NĂNG LƯỢNG", "ENERGY") },
        { "HUD_SHARDS", ("MẢNH LINH HỒN", "AETHEL SHARDS") },
        { "HUD_POTIONS", ("BÌNH MÁU", "POTIONS") },
        { "HUD_WAVE", ("ĐỢT QUÁI", "WAVE") },
        { "HUD_BOSS_ALERT", ("CẢNH BÁO: TRÙM ĐANG ĐẾN!", "WARNING: BOSS INCOMING!") },
        { "HUD_VICTORY", ("CHIẾN THẮNG!", "VICTORY!") },
        { "HUD_DEFEAT", ("BẠN ĐÃ TỬ TRẬN", "YOU DIED") },
        { "GAMEOVER_TITLE", ("BẠN ĐÃ TỬ TRẬN", "YOU DIED") },
        { "GAMEOVER_SUBTITLE", ("Linh hồn của bạn tan biến vào hư vô tàn tích...", "Your soul fades into the endless ruins...") },
        { "GAMEOVER_RESPAWN_AVAILABLE", ("HỒI SINH (CÒN 1 LẦN)", "RESPAWN (1 REMAINING)") },
        { "GAMEOVER_RESPAWN_USED", ("ĐÃ HẾT LƯỢT HỒI SINH", "NO RESPAWNS LEFT") },
        { "GAMEOVER_RESTART_MAP", ("CHƠI LẠI BẢN ĐỒ", "RESTART MAP") },
        { "GAMEOVER_MAIN_MENU", ("VỀ MENU CHÍNH", "MAIN MENU") },
        { "GAMEOVER_EXIT_LOGIN", ("VỀ MÀN HÌNH ĐĂNG NHẬP", "EXIT TO LOGIN") },
        { "HUD_CLEAR_TIME", ("Thời Gian Hoàn Thành:", "Clear Time:") },
        { "HUD_ENEMIES_SLAIN", ("Kẻ Thù Tiêu Diệt:", "Enemies Slain:") },
        { "HUD_SHARDS_EARNED", ("Linh Hồn Thu Được:", "Shards Earned:") },

        // Prompts & Interactions (Mobile touch / Proximity)
        { "PROMPT_SHRINE", ("Tế Đàn Cổ Đại", "Ancient Shrine") },
        { "PROMPT_CHEST", ("Mở Rương Báu", "Open Treasure Chest") },
        { "PROMPT_PORTAL", ("Cổng Không Gian", "Ruin Portal") },
        { "PROMPT_GRAVE", ("Thu Hồi Mảnh Linh Hồn", "Retrieve Lost Shards") },

        // Shrine Upgrades
        { "SHRINE_TITLE", ("TẾ ĐÀN NÂNG CẤP CỔ ĐẠI", "ANCIENT UPGRADE SHRINE") },
        { "UPGRADE_ATK", ("Cường Hóa Tấn Công (+10%)", "Upgrade Attack (+10%)") },
        { "UPGRADE_HP", ("Tăng Lực Máu Tối Đa (+25 HP)", "Upgrade Max Health (+25 HP)") },
        { "UPGRADE_SPD", ("Gia Tăng Tốc Độ (+15%)", "Upgrade Move Speed (+15%)") },
        { "UPGRADE_ULTI", ("Giảm Hồi Chiêu Tuyệt Kỹ (-10%)", "Upgrade Ultimate Cooldown (-10%)") },
        { "UPGRADE_COST", ("Chi Phí:", "Cost:") },
        { "MSG_NOT_ENOUGH_SHARDS", ("Không đủ Mảnh Linh Hồn!", "Not enough Aethel Shards!") },
        { "MSG_UPGRADE_SUCCESS", ("Nâng cấp thành công!", "Upgrade Successful!") },

        // Victory Screen
        { "VICTORY_TITLE", ("CHIẾN THẮNG!", "REALM PURIFIED!") },
        { "VICTORY_SUBTITLE", ("Tàn tích cổ đại đã được thanh tẩy khỏi sự tha hóa.", "The ancient ruins have been cleansed of corruption.") },
        { "CHEST_LOOT_TITLE", ("RƯƠNG BÁU TÀN TÍCH", "ANCIENT RUIN CHEST") },
        { "CHEST_LOOT_SUB", ("Chạm vào vật phẩm để lấy riêng, hoặc nhận toàn bộ:", "Tap an item to loot, or claim everything:") },
        { "BTN_CLAIM_ALL", ("LẤY TẤT CẢ", "CLAIM ALL") },
        { "BTN_TAKE_ITEM", ("NHẬN", "LOOT") }
    };

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (localizedStrings.TryGetValue(key, out var val))
        {
            return CurrentLanguage == Language.Vietnamese ? val.vi : val.en;
        }
        return key;
    }

    public static void Register(string key, string vi, string en)
    {
        if (!string.IsNullOrEmpty(key))
        {
            localizedStrings[key] = (vi, en);
        }
    }
}
