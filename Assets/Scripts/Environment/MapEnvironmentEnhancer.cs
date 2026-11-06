using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Map Environment Enhancer (Trình Điều Phối Môi Trường Bản Đồ)
/// 
/// ĐẢM BẢO CHUẨN ZERO-BUG & ZERO-DESTRUCTIVE:
/// 1. Tuyệt đối KHÔNG xóa bỏ hay thay đổi bất kỳ Tilemap hay Scene Object nào đã vẽ sẵn.
/// 2. Bổ sung các yếu tố tương tác động (Bẫy gai, Thùng nổ, Cột thạch anh băng, Đài tế, Bầu không khí)
///    vào đúng các vị trí chiến thuật hợp lý của Map 1, Map 2, Map 3.
/// </summary>
public static class MapEnvironmentEnhancer
{
    public static void EnsureEnhancements()
    {
        if (GameObject.Find("MapEnvironmentEnhancement") != null) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "LoginScene" || currentScene == "MainMenuScene") return;

        GameObject root = new GameObject("MapEnvironmentEnhancement");

        // 1. Kích hoạt hiệu ứng Bầu không khí điện ảnh (Vệt nắng, Tàn tro, Bụi tuyết)
        MapAtmosphereFXManager.EnsureManager();

        // 2. Trang bị bẫy và vật thể tương tác theo từng Map
        if (currentScene == "Map1_Thestart" || currentScene == "SampleScene")
        {
            SetupMap1Enhancements(root.transform);
        }
        else if (currentScene.Contains("Map2") || currentScene.Contains("Valley"))
        {
            SetupMap2Enhancements(root.transform);
        }
        else if (currentScene.Contains("Map3") || currentScene.Contains("Forgotten") || currentScene.Contains("Ancient"))
        {
            SetupMap3Enhancements(root.transform);
        }
    }

    private static void SetupMap1Enhancements(Transform parent)
    {
        // A. Bẫy gai sàn cổ đại ở các nút giao & hành lang
        FloorSpikeTrap.Create(new Vector3(0f, -1.8f, 0f), parent);
        FloorSpikeTrap.Create(new Vector3(5.5f, 0.8f, 0f), parent);
        FloorSpikeTrap.Create(new Vector3(-5.5f, 0.8f, 0f), parent);
        FloorSpikeTrap.Create(new Vector3(11.5f, -0.5f, 0f), parent);

        // B. Thùng thuốc nổ cổ xưa ở các góc phòng
        ExplosiveBarrel.Create(new Vector3(4.2f, 3.2f, 0f), parent);
        ExplosiveBarrel.Create(new Vector3(-4.2f, -3.0f, 0f), parent);
        ExplosiveBarrel.Create(new Vector3(13.5f, 2.5f, 0f), parent);
        ExplosiveBarrel.Create(new Vector3(-9.5f, 1.2f, 0f), parent);

        // C. Đài Tế Thánh Thể (Sanctuary Blessing Shrine) ở sảnh bắt đầu
        InteractiveAltarShrine.Create(new Vector3(-8.5f, 0f, 0f), InteractiveAltarShrine.AltarType.SanctuaryBlessing, parent);
    }

    private static void SetupMap2Enhancements(Transform parent)
    {
        // A. Bẫy gai sàn dọc hẻm thung lũng đá
        FloorSpikeTrap.Create(new Vector3(-3.2f, 0f, 0f), parent);
        FloorSpikeTrap.Create(new Vector3(2.5f, -1.2f, 0f), parent);
        FloorSpikeTrap.Create(new Vector3(7.2f, 1.6f, 0f), parent);
        FloorSpikeTrap.Create(new Vector3(-6.5f, -2.2f, 0f), parent);

        // B. Thùng thuốc nổ dầu hỏa cạnh các mỏm đá nham thạch
        ExplosiveBarrel.Create(new Vector3(-1.8f, 2.6f, 0f), parent);
        ExplosiveBarrel.Create(new Vector3(3.6f, -2.6f, 0f), parent);
        ExplosiveBarrel.Create(new Vector3(8.2f, -0.8f, 0f), parent);
        ExplosiveBarrel.Create(new Vector3(-7.2f, 2.8f, 0f), parent);

        // C. Đài Hiến Tế Huyết Nguyệt (Blood Sacrifice Altar)
        InteractiveAltarShrine.Create(new Vector3(5.5f, 3.2f, 0f), InteractiveAltarShrine.AltarType.BloodSacrifice, parent);
    }

    private static void SetupMap3Enhancements(Transform parent)
    {
        // A. Cột Thạch Anh Băng Hư Không tạo vỏ bọc che chắn đạn và nổ Frost Nova
        IceStalagmiteCrystal.Create(new Vector3(-5.2f, 2.2f, 0f), parent);
        IceStalagmiteCrystal.Create(new Vector3(5.2f, 2.2f, 0f), parent);
        IceStalagmiteCrystal.Create(new Vector3(-4.5f, -3.2f, 0f), parent);
        IceStalagmiteCrystal.Create(new Vector3(4.5f, -3.2f, 0f), parent);
        IceStalagmiteCrystal.Create(new Vector3(0f, 4.2f, 0f), parent);
        IceStalagmiteCrystal.Create(new Vector3(0f, -4.5f, 0f), parent);
    }
}

