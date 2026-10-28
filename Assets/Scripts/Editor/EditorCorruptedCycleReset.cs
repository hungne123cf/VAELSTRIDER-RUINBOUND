#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tự động reset trạng thái Chu Kỳ Tha Hóa về Chế Độ Thường (Normal Mode) khi mở Editor
/// </summary>
public static class EditorCorruptedCycleReset
{
    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        CorruptedCycleManager.ResetToNormalCycle();
        PlayerPrefs.DeleteKey("Vaelstrider_CorruptedCycle_Active");
        PlayerPrefs.DeleteKey("Vaelstrider_CorruptedCycle_Number");
        PlayerPrefs.Save();
        Debug.Log("<color=green>[CALAMITY RESET]</color> Đã xóa key PlayerPrefs và reset Chu Kỳ về Chế độ Thường!");
    }

    [MenuItem("Tools/Reset Calamity To Normal Mode")]
    public static void ManualReset()
    {
        CorruptedCycleManager.ResetToNormalCycle();
        PlayerPrefs.DeleteKey("Vaelstrider_CorruptedCycle_Active");
        PlayerPrefs.DeleteKey("Vaelstrider_CorruptedCycle_Number");
        PlayerPrefs.Save();
        Debug.Log("<color=green>[CALAMITY RESET]</color> Đã thủ công reset Chu Kỳ về Chế độ Thường!");
    }
}
#endif
