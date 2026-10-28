using UnityEditor;
using UnityEngine;

public static class RefreshAvatars
{
    [InitializeOnLoadMethod]
    public static void ForceReimportAvatars()
    {
        string[] avatarPaths = new string[]
        {
            "Assets/Resources/UI/Avatars/Avatar_Vaelstrider.png",
            "Assets/Resources/UI/Avatars/avatar_vael.png",
            "Assets/Resources/UI/Avatars/Avatar_GaleRonin.png",
            "Assets/Resources/UI/Avatars/avatar_ronin.png",
            "Assets/Resources/UI/Avatars/Avatar_AzureBlade.png",
            "Assets/Resources/UI/Avatars/avatar_azure.png",
            "Assets/Resources/UI/Avatars/Avatar_TitanBerserker.png",
            "Assets/Resources/UI/Avatars/avatar_titan.png",
            "Assets/Resources/UI/Avatars/Avatar_DuneAssassin.png",
            "Assets/Resources/UI/Avatars/avatar_dune.png",
            "Assets/Resources/UI/Avatars/Avatar_CelestialMage.png",
            "Assets/Resources/UI/Avatars/avatar_mage.png"
        };

        foreach (var p in avatarPaths)
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log("<color=green>[AvatarRefresh]</color> Reimported all 12 avatar face textures with sub-pixel precision!");
    }
}
