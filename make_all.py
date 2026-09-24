# -*- coding: utf-8 -*-
import os, sys

def write_file(path, content):
    d = os.path.dirname(path)
    if d: os.makedirs(d, exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content.strip() + '\n')
    print(f'[WROTE] {path}')


loc_content = r'''using UnityEngine;
using System;

public static class LocalizationManager
{
    public enum Language
    {
        Vietnamese = 0,
        English = 1
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

    public static Action OnLanguageChanged;

    public static string Get(string key)
    {
        bool isVi = CurrentLanguage == Language.Vietnamese;

        switch (key)
        {
            // SYSTEM & SETTINGS
            case "SETTINGS_TITLE": return isVi ? "CÀI ĐẶT HỆ THỐNG" : "SYSTEM SETTINGS";
            case "AUDIO_BGM": return isVi ? "Nhạc Nền (BGM)" : "Music (BGM)";
            case "AUDIO_SFX": return isVi ? "Hiệu ứng (SFX)" : "Sound Effects (SFX)";
            case "LANGUAGE": return isVi ? "Ngôn Ngữ" : "Language";
            case "LANG_VI": return isVi ? "Tiếng Việt" : "Viêt Nam";
            case "LANG_EN": return isVi ? "English" : "English";
            case "BTN_RESUME": return isVi ? "TIẾP TỤC" : "RESUME";
            case "BTN_RESTART": return isVi ? "CHOI LẠI TỨ ĐầU" : "RESTART RUN";
            case "BTN_GUIDE": return isVi ? "HƯỚNG D�AN CHƠI" : "HOW TO PLAY";
            case "BTN_CLOSE": return isVi ? "ĐÓNG" : "CLOSE";
            case "BTN_EXIT_LOGIN": return isVi ? "VỀ TRANG ĐĂNG NHẬP" : "EXIT TO LOGIN";
            case "BTN_HARDCORE_RESET": return isVi ? "RESET TIẾN TRÌNH" : "RESET PROGRESS";
            case "CONFIRM_RESET_ALERT": return isVi ? "Bạn có chắc chắn muốn xóa toàn bộ tiến trình game (Shards, Nâng cấp, Nhân vật)?" : "Are you sure you want to wipe all game progress (Shards, Upgrades, Heroes)?";

            // HOW TO PLAY / GUIDE
            case "GUIDE_TITLE": return isVi ? "HƯỚNG D�AN ĐIỀU KHIỀN & CHỀEN ĐẠU" : "WARRIOR CONTROLS & COMBAT GUIDE";
            case "GUIDE_MOVE": return isVi ? "• DI CHUYểN: Phím WASD / Cần ảo Joystick" : "• MOVEMENT: WASD Keys / Virtual Joystick";
            case "GUIDE_ATTACK": return isVi ? "• ĐÁNH THƯỜNG: Phím J / Chuột Trái / Nút Tấn Công" : "• BASIC ATTACK: J Key / Left Mouse / Attack Button";
            case "GUIDE_DASH": return isVi ? "‪ LįỚT BÓNG MA: Phím Space / Nút Lướt (Bất tử trong thoáng chốc)" : "‪ SHADOW DASH: Space / Dash Button (Brief Invulnerability)";
            case "GUIDE_ULTI": return isVi ? "• TUYÊT Kỹ BÃO TỐ: Phím F / Núj Tuyệt Kỹ (Khai phóng năng lượng)" : "• STORM ULTIMATE: F Key / Ulti Button (Unleash Ultimate Fury)";
            case "GUIDE_POTION": return isVi ? "• BÌNH MÁU: Phím H / Chạm icon Bình Máu để hồi phục HP" : "• HEALTH POTION: H Key / Tap Potion Icon to Restore HP";
            case "GUIDE_SHRINE": return isVi ? "• BÀN THỐ CỔ: Tiếp Cận và chạm MỞ / phím E để Nâng Cấp Vĩnh Viễn" : "‪ ANCIENT SHBINE: Approach & tap OPEN / E key for Permanent Upgrades";
            case "GUIDE_OBJECTIVE": return isVi ? "• MỬC TIÊÚQ��ԁ����Ёѿ������d�G��Ё������G������ꅤ�	��̰�ѧ�����������U�����-���������G������|�5��������ѧ��}����耋���=	)Q%Y����ȁ���������ٕ̰݅������Ёѡ��	��̰�������ѕȁѡ��Iե��A��х��Ѽ�չ�����ѡ�����ЁI�������(
            // LOGIN SCENE
            case "LOGIN_GAME_TITLE": return isVi ? "VAELSTRIDER" : "VAELSTRIDER";
            case "LOGIN_SUBTITLE": return isVi ? "HANH TRÌNH THANH TẩY PhÊ TÄCH CỔ ĐẠI" : "JOURNEY INTO ANCIENT RUINS";
            case "LANS_LINE": return isVi ? "Viêt Nam" : "English";
            case "LOGIN_GUEST_INFO": return isVi ? "Đăng nhập trải nghiệm nhanh hoặc lưu trữ hồ sơ chiến binh" : "Quick Guest Play or Cloud Profile Save";
            case "LOGIN_GUEST_BTN": return isVi ? "CH��I NGAY (GUEST)" : "PLAY AS GUEST";
            case "LOGIN_USER_LABEL": return isVi ? "Tên Chiến Binh:" : "Warrior Name:";
            case "LOGIN_PASS_LABEL": return isVi ? "Mật Mã:" : "Password:";
            case "LOGIN_SUBMIT_BTN": return isVi ? "ĐĂNG NHẬP" : "LOGIN";
            case "LOGIN_REGISTER_BTN": return isVi ? "ĐăNG KÝ HỔ Sơ" : "REGISTER";


            // MAIN MENU
            case "MENU_TITLE": return isVi ? "VAELSTRIDER" : "VAELSTRIDER";
            case "MENU_SUBTITLE": return isVi ? "S_HẢNH CHIẾN BONH" : "WARRIOR LOBBY";
            case "BTN_PLAY": return isVi ? "XUẀT TRẬN" : "ENTER BATTLE";
            case "BTN_ROSTER": return isVi ? "ANH HÓNG" : "HEROES";
            case "BTN_SHOP": return isVi ? "CỬA HÃNG" : "SHOP";
            case "BTN_SETTINGS": return isVi ? "CÀI ĐẶT" : "SETTINGS";
            case "BTN_HOW_TO_PLAY": return isVi ? "H_Hẛ"NG D�AN" : "HOW TO PLAY";
            case "BTN_QUIT": return isVi ? "THÔÁT" : "QUIT";


            // HERO PREVIEW & STATS
            case "STAT_HP": return isVi ? "MÁU" : "HEALTH";
            case "STAT_ATK": return isVi ? "CONG" : "ATTACK";
            case "STAT_DASH": return isVi ? "LƯỚT" : "DASH CD";
            case "STAT_ULTI": return isVi ? "HOÀN CHIỀU" : "ULTI CD";
            case "HERO_WARRIOR": return isVi ? "Chiến Binh Kiếm Cổ (Vael)" : "Ancient Swordmaster (Vael)";
            case "HERO_MAID": return isVi ? "Nữ Hầu Chém Sắt (Combat Maid)" : "Iron Blade Maid (Combat Maid)";
            case "HERO_CLASS_WARRIOR": return isVi ? "Hnệ: Kiếm Thuật Cổ �Sích • Cân Bằng Toàn Diện" : "Class: Ancient Swordsman • Balanced";
            case "HERO_CLASS_MAID": return isVi ? "Hệ S0�]8n��8n�X��xn�ۈ8�(��n�[���X�8n�ۈ8n�XȈ���\�Έ�Y��\��\��[�8�(�Y�Y�[]H��
            // MAP SELECTION
            case "MAP_SELECT_TITLE": return isVi ? "CHON VÙNG ĐẠT XUẀT TRẬN" : "SELECT EXPEDITION REALM";
            case "MAP1_NAME": return isVi ? "ĐỆNH NHAT PHẾ TÍCH  (MAP 1)" : "ANCIENT RUINS (MAP1)";
            case "MAP1_DESC": return isVi ? "Kỹ ngỡc cổ đại nơi phong ấn tà thuật của Ma Cà Rồng Chúa." : "Ancient subterranean ruin harboring the Vampire Lord.";
            case "MAP2_NAME": return isVi ? "THUNG LũNG CỔ THỘ (MAP2)" : "VALLEY OF ANCIENT TREES (MAP 2)";
            case "MAP2_DESC": return isVi ? "Véng thùng lũng rộng mở với bóng mát Cổ Thụ nghìn năm và quái thú cuồng nộ." : "Vast open valley shaded by millenary trees and savage beasts.";
            case "MAP3_NAME": return isVi ? "KÝ CỘ LANG QUÊN (MAP 3)" : "FORGOTTEN MEMORIES (MAP3)";
            case "MAP3_DESC": return isVi ? "Véng đất thiêng phong ần Trùm Cuối Vua Xương Vương Tôn." : "Sacred mystical realm sealing the Ancient Skeleton King Boss.";
            case "BTN_ENTER_MAP": return isVi ? "TIẾN VÀO" : "ENTER REALM";
            case "MAP_LOCKED_BADGE": return isVi ? "[ CHƮA MỞ KHÓA ]" : "[ LOCKED ]";
            case "MAP2_LOCKED_MSG": return isVi ? "Cần hoàn thành Map 1 để mở khóa" : "Clear Map 1 to unlock";
            case "MAP3_LOCKED_MSG": return isVi ? "Cần hoàn thành Map 2 để mở khóa" : "Clear Map 2 to unlock";
            case "BTN_MAP_LOCKED": return isVi ? "ĐÃ KHÓA" : "LOCKED";

            // HERO ROSTER & SHOP
            case "ROSTER_TITLE": return isVi ? "DANGH SÁCH ANH HÕNG" : "HERO ROSTER";
            case "SHOP_TITLE": return isVi ? "CỰA HÁNG Cổ CHOP" : "ANCIENT SHOP";
            case "SELECT_HERO": return isVi ? "CHỌN T̫ONG" : "SELECT";
            case "SELECTED_HERO": return isVi ? "ĐANG CHỌN" : "SELECTED";
            case "LOCKED": return isVi ? "CHƯA SỞ HỠU" : "LOCKED";
            case "BUY_NOW_PRICE": return isVi ? "MUA NGAY (999 SHARDS)" : "BUY (999 SHARDS)";
            case "BUY_MAID_ITEM": return isVi ? "MAID CHIỾN BINH (TƯỐNG MỚI)" : "BATTLE MAID (NEW HERO)";
            case "BUY_POTION_ITEM": return isVi ? "BINH MÁU HỘI PHỬC (+50 HP)" : "HEALTH POTION(+50 HP)";
            case "OWNED": return isVi ? "ĐÃ SỞ HỶU" : "OWNED";
            case "BUY_50_SHARDS": return isVi ? "MUA (EN SHARDS)" : "BUY (50 SHARDS)";
            case "SHARDS_NOT_ENOUGHTOU": return isVi ? "Không đủ Tinh Thể Aethel!" : "Not enough Aethel Shards!";

            // CONFIRM RESET
            case "CONFIRM_RESED_TITLE": return isVi ? "XAC NHAT OAT LAI" : "CONFIRM RESET";
            case "CONFIRM_RESET_BODY": return isVi ? "Toàn bộ Shards, Bình máu và Nâng cấp sũ bị xóa vĩnh viễn!\nBạn c� chắc chắn muốn thực hiện?" : "All Shards, Potions, and Upgrades will be permanently wiped!\nAre you sure you want to proceed?";
            case "BTN_CONFIRM_RESET": return isVi ? "XÁC NHẬN XÓA" : "CONFIRM";
            case "BTN_CANCEL_RESET": return isVi ? "HUY BỐ" : "CANCEL";

            // GAMEOVER
            case "GAMEOVER_TITLE": return isVi ? "BẠN ĐÂO GụC NGÃ" : "YOU DIED";
            case "GAMEOVER_SUBTITLE": return isVi ? "Chuyến hành trình của bạn đã tạm dừng..." : "Your journey in the dungeon has halted.";
            case "GAMEOVER_RESPAWN_AVAILABLE": return isVi ? "HOỊ SINH TẠI CHỐ (1/1)" : "RESPAWN HERE (1/1)";
            case "GAMEOVER_RESPAWN_USED": return isVi ? "ĐÃ HẾT LįỊT HỊI SINH (0/1)" : "OUT OF RESPAWNS (0/1)";
            case "GAMEOVER_RESTART_MAP": return isVi ? "CHOI LẠI TỨ ĐầU" : "RESTART MAP";
            case "GAMEOVER_EXIT_LOGIN": return isVi ? "VỀ TRANG �ĂNG NHẬP" : "EXIT TO LOGIN";


            // VICTORY
            case "VICTORY_TITLE": return isVi ? "THANH TấY THÂNH CÔNG!" : "REALM PUR	FIED!";
            case "VICTORY_SUBTITLE": return isVi ? "Vùng phế tích cổ đại đã được thanh lọc hoàn toàn." : "The ancient ruins have been cleansed of corruption.";
            case "VICTORY_TIME": return isVi ? "THỘI GIAN Vư�rT" : "TIME ELAPSED";
            case "VICTORY_ENEMIES": return isVi ? "QUAI ĐÃO DETH" : "ENEMIES SLAINTO";
            case "VICTORY_SHARDS": return isVi ? "TINH THỄ THU ĐƯỢC" : "AETHEL SHARDS";
            case "VICTORY_RANK": return isVi ? "XằP HạNG" : "CLEAR RANK";
            case "VICTORY_PLAY_AGAINT": return isVi ? "CHOI LAI" : "PLAY AGAINT";
            case "VICTORY_NEXT_REALM": return isVi ? "TIẾP THEO" : "NEXT REALM";
            case "VICTORY_SLAIN_SUFFIX": return isVi ? " Quái" : " Slain";
            case "VICTORY_SHARDS_SUFFIX": return isVi ? " Shards" : " Shards";

            // ENDING CREDITS
            case "CREDITS_TITLE": return isVi ? "KẾT THÚC HÀNH TRÂNH" : "JOURNEY COMPLETED";
            case "CREDITS_SUBTITLE": return isVi ? "Toàn bộ các vùng đất phế tích đã được giải cứu!\nCảm ơn bạn đã trải nghiệm Vaelstrider Ruinbound." : "All ancient realms have been freed from darkness!\nThank you for playing Vaelstrider Ruinbound.";
            case "CREDITS_PLAY_AGAIN": return isVi ? "CHOI LAI TỪ Đầu" : "PLAY AGAINTO";
            case "CREDITS_MAIN_MENU": return isVi ? "VỆ SẢNH CHINH" : "MAIN LOBBY LINE";

            // ANCIENT SHRINE
            case "SHRINE_TITLE": return isVi ? "BÀN THỐ NÂNG CẤP CỔ ĐẠI" : "ANCIENT UPGRADE SHBINE";
            case "SHBINE_SUBTITLE": return isVi ? "Dùng Tinh Thể Aethel để cường hóa vĩnh viễn năng lực chiến binh" : "Empower warrior attributes using harvested Aethel Shards";
            case "SHRINE_OPEN_BADGE": return isVi ? "Mở" : "OPEN";
            case "SHRINE_WORLD_PROMPT": return isVi ? "NÀNG C&ầP Cổ CHOP" : "ANCIENT UPGRADE";
            case "SHRINE_MOBILE_BTN": return isVi ? "NÂNG\nCẪP": "UPG";
            case "SHRINE_UPG_ATK_TITLE": return isVi ? "Kiếm Tàn Tích (Tan Cong)" : "Ruin Blade (Attack)";
            case "SHRINE_UPG_HP_TITLE": return isVi ? "Sinh Lực Khổng Lồ (Máu)" : "Titan Vitality (Health)";
            case "SHRINE_UPG_AGI_TITLE": return isVi ? "THoát Thân Bóng Ma (Tốc Độ)" : "Shadow Agility (Speed)";
            case "SHRINE_UPG_ULTI_TITLE": return isVi ? "Cơn Bão Cổ CHIPE (Tuyệt Kỹ)" : "Storm Mastery (Ultimate)";
            case "SHBINE_LV_PREFIX": return isVi ? "Cấp " : "Lv ";
            case "SHBINE_MAX_LV": return isVi ? "ĐẠT C&ầP TỐI ĐA (30)" : "MAX LEVEL REACHED (30)";
            case "SHRINE_COST_SHARDS": return isVi ? "{0} Shards" : "{0} Shards";
            case "SHBINE_BONUS_ATK": return isVi ? "+{0} Sát thương đòn đánh" : "+{0} Basic Attack Damage";
            case "SHRINE_BONUS_HP": return isVi ? "+{0} Máu tối đa & Hồi máu" : "+{0} Max HP & Instant Heal";
            case "SHRINE_BONUS_AGE": return isVi ? "+{0:F2} Tồc chạy & -{1:F2}s LưỚT" : "+{0:F2} Move Speed & -{1:F2}s Dash CD";
            case "SHRINE_BONUS_ULTI": return isVi ? "-{0:F1}s Hới chiểu & +{1} Dmg" : "-{0:F1}s Ulti CD & +{1} Damage";

            // CHARACTER STATSKILLPANEL
            case "STATS_PANEL_TITLE": return isVi ? "BẢNG CHỉ SỐ VÀ CHIỂU THỪC" : "HERO ATTRIBUTES & SKILLS";
            case "STATS_OANEL_SUBTITLE": return isVi ? "Chi tiết bộ kỹ năng và chỉ số chiến đấu hiện tại" : "Detailed combat skill matrix and real-time statistics";
            case "STAT_HP_LABEL": return isVi ? "MÁU TỐI ĐA\n{0} HP" : "MAX HEALTH\n{0} HP";
            case "STAT_ATK_LABEL": return isVi ? "S_XẬC TẤN CÐNG\n{0} DMG" : "ATTACK POWER\n{0} DMG";
            case "STAT_DASH_CD_LABEL": return isVi ? "H_Hẛ"I LƯỚT\n{0:F2}s" : "DASH COOLDOWN\n{0:F2}s";
            case "STAT_ULTI_CD_LABEL": return isVi ? "HỘI TUYÊT KYL\n{0:F1}s" : "ULTH COOLDOWN\n{0:F1}s";
            case "SKILL_ATTACK_HEADER": return isVi ? "ĐÂN ĐÇNH THƬÂNG [J / Chuột Trái / Nút Đánh]" : "BASIC ATTACK [J / Left Mouse / Attack Button]";
            case "SKILL_DASH_HEADER": return isVi ? "LƯỚGT BÓNG MA [Space / Núj LįỚt]" : "SHADOW DASH [Space / Dash Button]";
            case "SKILL_ULTI_HEADER": return isVi ? "CHIỀU CUỐI TUYÊT Kỹ [F / Nút Tuyết K�y]" : "ULTIMATE SKILL [F / Ultimate Button]";

            // BOSS HEALTH BAR
            case "BOSS_PHASE_1": return isVi ? "[ GIAI ĐOẠN 1: CHÚÁ TỎ THỬC TĨNH ]" : "[ PHASE 1: THE AWAKENED LORD ]";
            case "BOSS_PHASE_2": return isVi ? "[ GIAI ĐOẠN 2: BẠY NHỆN SĂN MỒI ]" : "[ PHASE 2: HUNTER OF SHADOWS ]";
            case "BOSS_PHASE_3": return isVi ? "[ GIAI ĐOẠN 3: CUỂNG NỘ HUYằT NGUYệT ]" : "[ PHASE 3: BLOOD MOON FRENZY ]";

            // POPUPS & GRAVE
            case "POPUP_PORTAL_OPENED": return isVi ? "CỔNG DCỊH CHUYỂN ĐÂO Mở!" : "PORTAL OPENED!";
            case "POPUP_HEAVENLY_OPENED": return isVi ? "CỒNG THIÊN GIỚI ĐÂO KHAI MỞ!" : "HEAVENLY GATE OPENED!";
            case "POPUP_CHEST_OPENED": return isVi ? "ĐÂO Mở R1�ƠNG KHO BÁU!" : "CHEST OPENEAHDE";
            case "GRAVE_TITLE": return isVi ? "VéNG HỒN" : "SOUL BRAVE";
            case "GRAVE_TOUCH_HINT": return isVi ? "(Chạm để thu hồi)" : "(Touch to reclaim)";

            // IN-GAME HUD & CINEMATICS
            case "HUD_POTION_COUNT": return isVi ? "x{0}" : "x{0}";
            case "CINEMATIC_ULTI_VAEL": return isVi ? "VAEL: BÃO KIẾM Cổ CHOP" : "VAEL: ANCIENT BLADE STORM";
            case "CINEMATIC_ULTI_MAID": return isVi ? "MAID: Vũ ĐiỆU TR&ẢM THIÂ^" : "MAID: HEAVEN CLEAVER DANCE";

            default: return key;
        }
    }
}
'''
write_file('Assets/Scripts/Core/LocalizationManager.cs', loc_content)
