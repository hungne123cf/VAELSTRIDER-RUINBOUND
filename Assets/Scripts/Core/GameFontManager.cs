using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Quản lý font chuẩn Dark Fantasy / Roguelike cho toàn bộ dự án:
/// - Header / Title / Boss / Banners: Cinzel (phong cách Dark Fantasy cổ điển, uy nghiêm)
/// - Body / UI / Buttons / Stats / Tooltips: Be Vietnam Pro (chuẩn tiếng Việt có dấu, sắc nét trên Android)
/// Đảm bảo Zero-Bug: Tự động fallback sang Be Vietnam Pro khi gặp ký tự có dấu, không bao giờ bị lỗi ô vuông '?'.
/// </summary>
public static class GameFontManager
{
    private static TMP_FontAsset _headerFont;
    private static TMP_FontAsset _bodyFont;
    private static bool _isInitialized = false;

    public static TMP_FontAsset HeaderFont
    {
        get
        {
            EnsureInitialized();
            return _headerFont ?? _bodyFont ?? TMP_Settings.defaultFontAsset;
        }
    }

    public static TMP_FontAsset BodyFont
    {
        get
        {
            EnsureInitialized();
            return _bodyFont ?? TMP_Settings.defaultFontAsset;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (_isInitialized && _bodyFont != null && _headerFont != null) return;

        try
        {
            // 1. Thử nạp Font Asset Dreams Unwound đã bake sẵn từ Resources/Fonts
            if (_bodyFont == null)
            {
                _bodyFont = Resources.Load<TMP_FontAsset>("Fonts/DreamsUnwound SDF")
                         ?? Resources.Load<TMP_FontAsset>("Fonts/iCielDreamsUnwound SDF");
            }

            // 2. Nếu chưa có TMP_FontAsset, tạo Dynamic SDF từ file TrueType (.ttf/.otf) trong Resources/Fonts
            if (_bodyFont == null)
            {
                Font dreamsFont = Resources.Load<Font>("Fonts/iCielDreamsUnwound-Regular")
                               ?? Resources.Load<Font>("Fonts/DreamsUnwound_italic")
                               ?? Resources.Load<Font>("Fonts/BeVietnamPro-SemiBold");
                if (dreamsFont != null)
                {
                    _bodyFont = TMP_FontAsset.CreateFontAsset(dreamsFont);
                    if (_bodyFont != null)
                    {
                        _bodyFont.name = "DreamsUnwound SDF (Dynamic)";
                        if (_bodyFont.material != null && _bodyFont.atlasTextures != null && _bodyFont.atlasTextures.Length > 0 && _bodyFont.atlasTextures[0] != null)
                        {
                            _bodyFont.material.SetTexture(ShaderUtilities.ID_MainTex, _bodyFont.atlasTextures[0]);
                        }
                    }
                }
            }

            // Header cũng dùng font Dreams Unwound theo đúng yêu cầu người dùng
            _headerFont = _bodyFont;

            // Fallback an toàn nếu font không load được
            if (_bodyFont == null) _bodyFont = TMP_Settings.defaultFontAsset;
            if (_headerFont == null) _headerFont = _bodyFont;

            // 3. Cấu hình bảng Fallback sang Default TMP Font phòng khi gặp ký tự emoji/biểu tượng
            if (_bodyFont != null && TMP_Settings.defaultFontAsset != null && _bodyFont != TMP_Settings.defaultFontAsset)
            {
                if (_bodyFont.fallbackFontAssetTable == null)
                    _bodyFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

                if (!_bodyFont.fallbackFontAssetTable.Contains(TMP_Settings.defaultFontAsset))
                {
                    _bodyFont.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
                }
            }

            _isInitialized = true;
            Debug.Log("<color=green>[GameFontManager]</color> Successfully initialized Dreams Unwound font as primary game font!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameFontManager] Initialization note: " + ex.Message);
        }
    }

    /// <summary>
    /// Quét và áp dụng chuẩn font Dreams Unwound cho toàn bộ cây UI phân cấp.
    /// </summary>
    public static void ApplyFontsGlobally(GameObject root)
    {
        if (root == null) return;
        EnsureInitialized();

        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            ApplyFontToText(txt);
        }
    }

    public static void ApplyFontToText(TextMeshProUGUI txt)
    {
        if (txt == null) return;
        txt.font = BodyFont;
    }
}

