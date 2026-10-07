using UnityEngine;
using UnityEditor;

public static class Player4AssetRefresher
{
    [MenuItem("Vaelstrider/Quick Refresh Assets")]
    public static void RefreshAssets()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("<color=green>[ASSET REFRESH]</color> AssetDatabase đã được làm mới hoàn tất!");
    }
}
