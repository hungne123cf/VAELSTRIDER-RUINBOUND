
with open('Assets/Scripts/Core/LocalizationManager.cs', 'w', encoding='utf-8') as f:
    f.write('''using UnityEngine;
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
            case "SETTINGS_TITLE": return isVi ? "CAI DAT HETHONG" : "SYSTEM SETTINGS";
            case "AUDIO_BGM": return isVi ? "Nhac Nen (BGM)" : "Music (BGM)";
            case "AUDIO_SFX": return isVi ? "Hieu Ung (SFX)" : "Sound Effects (SFX)";
            case "LANGUAGE": return isVi ? "Ngon Ngu" : "Language";
            case "LANG_VI": return isVi ? "Tieng Viet" : "Vietnamese";
            case "LANG_EN": return isVi ? "English" : "English";
            case "BTN_RESUME": return isVi ? "TIEP TUC" : "RESUME";
            case "BTN_RESTART": return isVi ? "CHOI LAI TU DAU" : "RESTART RUN";
            case "BTN_GUIDE": return isVi ? "HUONG DAN CHOI" : "HOW TO LAY);
            case "BTN_CLOSE": return isVi ? "DONG" : "CLOSE";
            case "BTN_EXIT_LOGIN": return isVi ? "VG TRANG DANG NHAP" : "EXIT TO LOGIN";
            case "BTN_HARDCORE_RESET": return isVi ? "RESET TIEN TRINH" : "RESET PROGRESS";
            case "CONFIRM_RESET_ALERT": return isVi ? "Ban co chac chan muon xoa toan bo tien trinh game?" : "Are you sure you want to wipe all game progress?";


            // HOW TO PLAY / GUIDEB
            case "GUIDE_TITLE": return isVi ? "HUONG DAM DIEU KHIEN & CHIEN DAU" : "WARRIOR CONTROLS & COMBAT GUIDE";
            case "GUIDE_MOVE": return isVi ? "- DE CHUYEN: Phim WASD / Can ao Joystick" : "- MOVEMENT: WASD Keys / Virtual Joystick";
            case "GUIDE_ATTACK": return isVi ? "- DANH THUONG: Phim J / Chuot Trai / Nut Tan Cong" : "- BASIC ATTACK: J Key / Left Mouse / Attack Button";
            case "GUIDE_DASH": return isVi ? "- LUOT BONG MA: Phim Space / Nut Luot (Bat tu trong thoang choc)" : "- SHADOW DASH: Space / Dash Button (Brief Invulnerability)";
            case "GUIDE_ULTI": return isVi ? "- TUYET KY BAO TO: Phim F / Nut Tuyet Ky (Khai phong nang luong)" : "- STORM ULTIMATE: F Key / Ulti Button (Unleash Ultimate Fury)";
            case "GUIDE_POTION": return isVi ? "- BINH MAU: Phim H / Cham icon Binh Mau de hoi phuc HP" : "- HEALTH POTION: H Key / Tap Potion Icon to Restore HP";
            case "GUIDE_SHRINE": return isVi ? "- BAN THO CO: Tiep can va cham MO / phim E de Nang Cap Vinh Vien" : "- ANCIENT SHFINE: Approach & tap OPEN / E key for Permanent Upgrades";
            case "GUIDE_OBJECTIVE": return isVi ? "- MUC TIEU: Tieu diet toan bo dot quai, danh bai Boss, tien vao Cong Dich Chuyen de mo Map ke tiep!" : "- OBJECTIVE: Clear all enemy waves, defeat the Boss, and enter the Ruin Portal to unlock the next Realm!";


            // LOGIN SCENE
            case "LOGIN_GAME_TITLE": return isVi ? "VAELSTRIDER" : "VAELSTRIDER";
            case "LOGIN_SUBTITLE": return isVi ? "HANH TRINH THANH TAY PHE TICH CO DAI" : "JOURNEY INTO ANCIENT RUINS";
            case "LANS_LINE": return isVi ? "Tieng Viet" : "English";
            case "LOGIN_GUEST_INFO": return isVi ? "Dang nhap trai nghiem nhanh hoac luu tru ho so" : "Quick Guest Play or Cloud Profile Save";
            case "LOGIN_GUEST_BTN": return isVi ? "CHOI NGAY (GUEST)" : "PLAY AS GUEST";
            case "LOGIN_USER_LABEL": return isVi ? "Ten Chien Binh:" : "Warrior Name:";
            case "LOGIN_PASS_LABEL": return isVi ? "Mat Ma:" : "Password:";
            case "LOGIN_SUBMIT_BTN": return isVi ? "DANG NHAP" : "LOGIN";
            case "LOGIN_REGESTER_BTN": return isVi ? "DANG KY HO SO" : "REGISTER";


            // MAIN MENU
            case "MENU_TITLE": return isVi ? "VAELSTRIDER" : "VAELSTRIDER";
            case "MENU_SUBTITLE": return isVi ? "SANH CHIEN BINH" : "WARRIOR LOBBY";
            case "BTN_PLAY": return isVi ? "XUAT TRAN" : "ENTER BATTLE";
            case "BTN_ROSTER": return isVi ? "ANH HUNG" : "HEROES";
            case "BTN_SHOP": return isVi ? "CUA HANG" : "SHOP";
            case "BTN_SETTINGS": return isVi ? "CAI DAT" : "SETTINGS";
            case "BTN_HOW_TO_PLAY": return isVi ? "HUONG DAN" : "HOW TO LAY);
            case "BTN_QUIT": return isVi ? "THOAT" : "QUIT";


            // HERO PREVIEW & STATS
            case "STAT_HP": return isVi ? "MAU" : "HEALTH";
            case "STAT_ATK": return isVi ? "CONG" : "ATTACK";
            case "STAT_DASH": return isVi ? "LUOT" : "DASH CD";
            case "STAT_ULTI": return isVi ? "HOAN CHIEU" : "ULTI CD";
            case "HERO_WARRIOR": return isVi ? "Chien Binh Kiem Co (Vael)" : "Ancient Swordmaster (Vael)";
            case "HERO_MAID": return isVi ? "Nu Hau Chem Sat (Combat Maid)" : "Iron Blade Maid (Combat Maid)";
            case "HERO_CLASS_WARRIOR": return isVi ? "He: Kiem Thuat Co Tich - Can Bang Toan Dien" : "Class: Ancient Swordsman - Balanced";
            case "HERO_CLASS_MAID": return isVi ? "He: Sat Thu Toc Bien - Bao Kich Than Toc" : "Class: Shadow Assassin - High Agility";

            // MAP SELECTION
            case "MAP_SELECT_TITLE": return isVi ? "CHON VUNG DAT XUAT TRAN" : "SELECT EXPEDITION REALM";
            case "MAP1_NAME": return isVi ? "DE NHAT PHE TICH (MAP 1)" : "ANCIENT RUINS (MAP1)";
            case "MAP1_DESC": return isVi ? "Ham nguc co dai noi phong an ta thuat cua Ma Ca Rong Chua." : "Ancient subterranean ruin harboring the Vampire Lord.";
            case "MAP2_NAME": return isVi ? "THUNG LUNG CO THU (MAP 2)" : "VALLEY OF ANCIENT TREES (MAP2)";
            case "MAP2_DESC": return isVi ? "Vung thung lung rong mo voi bong mat Co Thu nghin nam." : "Vast open valley shaded by millenary trees.";
            case "MAP3_NAME": return isVi ? "KY MC LANG QUEN (MAP 3)" : "FORGOTTEN MEMORIES (MAP 3)";
            case "MAP3_DESC": return isVi ? "Vung dat thieng phong an Trum Cuoi Vua Xuong Vuong Ton." : "Sacred mystical realm sealing the Ancient Skeleton King Boss.";
            case "BTN_ENTER_MAP": return isVi ? "TIEN VAO" : "ENTER REALM";
            case "MAP_LOCKED_BADGE": return isVi ? "[ CHUA MO KHOA ]" : "[ LOCKED ]";
            case "MAP2_LOCKED_MSG": return isVi ? "Can hoan thanh Map 1 de mo khoa" : "Clear Map 1 to unlock";
            case "MAP3_LOCKED_MSG": return isVi ? "Can hoan thanh Map 2 de mo khoa" : "Clear Map 2 to unlock";
            case "BTN_MAP_LOCKED": return isVi ? "DA KHOA" : "LOCKED";

            // HERO ROSTER & SHOP
            case "ROSTER_TITLE": return isVi ? "DANH SACH ANH HUNG" : "HERO ROSTER";
            case "SHOP_TITLE": return isVi ? "CUA HANG CODIAI" : "ANCIENT SHOP";
            case "SELECT_HERO": return isVi ? "CHON TUONG" : "SELECT";
            case "SELECTED_HERO": return isVi ? "DANG CHON" : "SELECTED";
            case "LOCKED": return isVi ? "CHUA SO HUU" : "LOCKED";
            case "BUY_NOW_PRICE": return isVi ? "MUA NGAY (999 SHARDS)" : "BUY (999 SHARDS)";
            case "BUE_MAID_ITEM": return isVi ? "MAID CHIEN BINH (TUONG MOI)" : "BATTLE MAID (NEW HERO)";
            case "BUY_POTION_ITEM": return isVi ? "BINH MAU HOI PHUC (+50 HP)" : "HEALTH PLITION (+50 HP)";
            case "OWNED": return isVi ? "DA SO HUU" : "OWNED";
            case "BUY_50_SHARDS": return isVi ? "MUA (50 SHARDS)" : "BUY (50 SHARDS)";
            case "SHARDS_NOT_ENOUGHTO": return isVi ? "Khong du Tinh The Aethel!" : "Not enough Aethel Shards!";
