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
            // 1. Thử nạp Font Asset đã bake sẵn từ Resources/Fonts
            if (_bodyFont == null)
            {
                _bodyFont = Resources.Load<TMP_FontAsset>("Fonts/BeVietnamPro SDF")
                         ?? Resources.Load<TMP_FontAsset>("Fonts/VaelstriderFont");
            }

            if (_headerFont == null)
            {
                _headerFont = Resources.Load<TMP_FontAsset>("Fonts/Cinzel SDF");
            }

            // 2. Nếu chưa có TMP_FontAsset, tạo Dynamic SDF từ file TrueType (.ttf) trong Resources/Fonts
            if (_bodyFont == null)
            {
                Font vnFont = Resources.Load<Font>("Fonts/BeVietnamPro-SemiBold");
                if (vnFont != null)
                {
                    _bodyFont = TMP_FontAsset.CreateFontAsset(vnFont);
                    if (_bodyFont != null)
                    {
                        _bodyFont.name = "BeVietnamPro SDF (Dynamic)";
                        if (_bodyFont.material != null && _bodyFont.atlasTextures != null && _bodyFont.atlasTextures.Length > 0 && _bodyFont.atlasTextures[0] != null)
                        {
                            _bodyFont.material.SetTexture(ShaderUtilities.ID_MainTex, _bodyFont.atlasTextures[0]);
                        }
                    }
                }
            }

            if (_headerFont == null)
            {
                Font cinzelFont = Resources.Load<Font>("Fonts/Cinzel-Variable");
                if (cinzelFont != null)
                {
                    _headerFont = TMP_FontAsset.CreateFontAsset(cinzelFont);
                    if (_headerFont != null)
                    {
                        _headerFont.name = "Cinzel SDF (Dynamic)";
                        if (_headerFont.material != null && _headerFont.atlasTextures != null && _headerFont.atlasTextures.Length > 0 && _headerFont.atlasTextures[0] != null)
                        {
                            _headerFont.material.SetTexture(ShaderUtilities.ID_MainTex, _headerFont.atlasTextures[0]);
                        }
                    }
                }
            }

            // Fallback an toàn nếu font không load được
            if (_bodyFont == null) _bodyFont = TMP_Settings.defaultFontAsset;
            if (_headerFont == null) _headerFont = _bodyFont;

            // 3. Cấu hình bảng Fallback: Header (Cinzel) -> Body (BeVietnamPro)
            if (_headerFont != null && _bodyFont != null && _headerFont != _bodyFont)
            {
                if (_headerFont.fallbackFontAssetTable == null)
                    _headerFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

                if (!_headerFont.fallbackFontAssetTable.Contains(_bodyFont))
                {
                    _headerFont.fallbackFontAssetTable.Add(_bodyFont);
                }
            }

            _isInitialized = true;
            Debug.Log("<color=green>[GameFontManager]</color> Successfully initialized Dark Fantasy fonts (Cinzel + Be Vietnam Pro)!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameFontManager] Initialization note: " + ex.Message);
        }
    }

    /// <summary>
    /// Quét và áp dụng chuẩn font cho toàn bộ cây UI phân cấp:
    /// Các text có tính chất tiêu đề/boss/gameover sẽ dùng HeaderFont (Cinzel).
    /// Các text còn lại (nút bấm, chỉ số, mô tả, cấp độ) sẽ dùng BodyFont (Be Vietnam Pro).
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

        string nameLower = txt.gameObject.name.ToLower();
        string textLower = (txt.text ?? "").ToLower();

        bool isHeader = nameLower.Contains("title") 
                     || nameLower.Contains("header") 
                     || nameLower.Contains("banner") 
                     || nameLower.Contains("bossname") 
                     || nameLower.Contains("gameover")
                     || textLower.Contains("kết thúc")
                     || textLower.Contains("tử trận")
                     || textLower.Contains("chiến thắng")
                     || textLower.Contains("linh hồn:");

        if (isHeader)
        {
            txt.font = HeaderFont;
            txt.fontStyle = FontStyles.Bold;
        }
        else
        {
            txt.font = BodyFont;
        }
    }
}

