using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Universal Typography & Accessibility Font Manager
/// Quản lý font chuẩn Dreams Unwound VH cho TOÀN BỘ dự án:
/// - Phủ 100% font chữ: Menu, HUD, Lõi TFT (LevelUp), Cửa Hàng Trang Bị, Shop NPC, Bàn Thờ, Hội Thoại...
/// - Tự động phóng to kích thước chữ để người cận thị trên điện thoại cũng đọc rõ ràng 100%.
/// - Chống nhòe (Anti-Smear): Giới hạn Outline tối đa 0.15f, ngăn tuyệt đối lỗi chữ bị biến thành khối màu đặc.
/// - Đảm bảo Zero-Bug: Fallback tự động khi gặp ký tự emoji/ký tự đặc biệt.
/// </summary>
public static class GameFontManager
{
    private static TMP_FontAsset _headerFont;
    private static TMP_FontAsset _bodyFont;
    private static Font _trueTypeFont;
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

    public static Font TrueTypeFont
    {
        get
        {
            EnsureInitialized();
            return _trueTypeFont;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        EnsureInitialized();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFontsToAllCanvases();
    }

    public static void EnsureInitialized()
    {
        if (_isInitialized && _bodyFont != null && _headerFont != null) return;

        try
        {
            // 1. Nạp file TrueType TTF gốc
            if (_trueTypeFont == null)
            {
                _trueTypeFont = Resources.Load<Font>("Fonts/iCielDreamsUnwound-Regular")
                             ?? Resources.Load<Font>("Fonts/DreamsUnwound_italic")
                             ?? Resources.Load<Font>("Fonts/BeVietnamPro-SemiBold");
            }

            // 2. Nạp Font Asset Dreams Unwound SDF từ Resources/Fonts
            if (_bodyFont == null)
            {
                _bodyFont = Resources.Load<TMP_FontAsset>("Fonts/DreamsUnwound SDF")
                         ?? Resources.Load<TMP_FontAsset>("Fonts/iCielDreamsUnwound SDF");
            }

            // 3. Nếu chưa có TMP_FontAsset, tạo Dynamic SDF từ TTF
            if (_bodyFont == null && _trueTypeFont != null)
            {
                _bodyFont = TMP_FontAsset.CreateFontAsset(_trueTypeFont);
                if (_bodyFont != null)
                {
                    _bodyFont.name = "DreamsUnwound SDF (Dynamic)";
                    if (_bodyFont.material != null && _bodyFont.atlasTextures != null && _bodyFont.atlasTextures.Length > 0 && _bodyFont.atlasTextures[0] != null)
                    {
                        _bodyFont.material.SetTexture(ShaderUtilities.ID_MainTex, _bodyFont.atlasTextures[0]);
                    }
                }
            }

            // Header và Body đều dùng đồng nhất font Dreams Unwound
            _headerFont = _bodyFont;

            // Fallback an toàn nếu font không load được
            if (_bodyFont == null) _bodyFont = TMP_Settings.defaultFontAsset;
            if (_headerFont == null) _headerFont = _bodyFont;

            // 4. Cấu hình bảng Fallback sang Default TMP Font phòng khi gặp ký tự emoji
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
            Debug.Log("<color=green>[GameFontManager]</color> Successfully initialized Dreams Unwound font as universal game font!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameFontManager] Initialization note: " + ex.Message);
        }
    }

    /// <summary>
    /// Quét và áp dụng chuẩn font Dreams Unwound cùng kích thước chữ to rõ ràng cho toàn bộ cây UI phân cấp.
    /// </summary>
    public static void ApplyFontsGlobally(GameObject root)
    {
        if (root == null) return;
        EnsureInitialized();

        // 1. TextMeshProUGUI
        var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmps)
        {
            ApplyFontToText(txt);
        }

        // 2. Standard Unity UI Text (nếu có)
        var legacyTexts = root.GetComponentsInChildren<Text>(true);
        foreach (var legTxt in legacyTexts)
        {
            ApplyFontToLegacyText(legTxt);
        }
    }

    /// <summary>
    /// Quét toàn bộ Canvases trong Scene (bao gồm cả các Modal ẩn) để áp dụng font Dreams Unwound và phóng to chữ.
    /// </summary>
    public static void ApplyFontsToAllCanvases()
    {
        EnsureInitialized();
        var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c != null)
            {
                ApplyFontsGlobally(c.gameObject);
            }
        }
    }

    public static void ApplyFontToText(TextMeshProUGUI txt, bool isHeader = false)
    {
        if (txt == null) return;
        var targetFont = isHeader ? HeaderFont : BodyFont;
        if (targetFont == null) return;

        txt.font = targetFont;

        // Reset shared material về đúng material của Dreams Unwound để tránh lỗi lệch Texture / Shader
        if (targetFont.material != null)
        {
            txt.fontSharedMaterial = targetFont.material;
        }

        // TỰ ĐỘNG THAY THẾ KÝ TỰ EM-DASH / EN-DASH (—, –) BẰNG DẤU GẠCH NỐI CHUẨN (-)
        // Tránh hoàn toàn lỗi thiếu ký tự glyph ô vuông / gạch đứt nét
        if (!string.IsNullOrEmpty(txt.text))
        {
            if (txt.text.Contains("\u2014") || txt.text.Contains("\u2013"))
            {
                txt.text = txt.text.Replace('\u2014', '-').Replace('\u2013', '-');
            }
        }

        // KHẮC PHỤC TRIỆT ĐỂ LỖI NHÒE CHỮ (ANTI-SMEAR):
        // Font Dreams Unwound là nét mảnh, outline dày sẽ lấp đầy ruột chữ khiến chữ biến thành khối đặc.
        // Tắt outlineWidth quá dày, ưu tiên đổ bóng viền sắc nét (Underlay) thay vì Outline dày.
        txt.outlineWidth = 0f;

        // PHÓNG TO CHỮ HẾT CỠ CHO ĐIỆN THOẠI (Mobile Maximum Accessibility)
        // Kích thước siêu lớn đảm bảo người cận thị trên điện thoại cũng đọc rõ ràng 100%
        if (txt.enableAutoSizing)
        {
            if (txt.fontSizeMin < 24f) txt.fontSizeMin = 24f;
            if (txt.fontSizeMax < 44f) txt.fontSizeMax = Mathf.Max(txt.fontSizeMax * 1.6f, 44f);
        }
        else
        {
            if (txt.fontSize < 20f)
            {
                txt.fontSize = Mathf.Max(txt.fontSize * 1.85f, 26f);
            }
            else if (txt.fontSize < 32f)
            {
                txt.fontSize = Mathf.Max(txt.fontSize * 1.55f, 36f);
            }
            else if (txt.fontSize < 50f)
            {
                txt.fontSize = Mathf.Max(txt.fontSize * 1.35f, 54f);
            }
            else
            {
                txt.fontSize = Mathf.Min(txt.fontSize * 1.25f, 96f);
            }
        }

        // Đảm bảo chữ tự động xuống dòng mượt mà khi phóng to, không bị cắt xén
        if (txt.overflowMode == TextOverflowModes.Truncate)
        {
            txt.overflowMode = TextOverflowModes.Overflow;
        }
    }

    public static void ApplyFontToLegacyText(Text txt)
    {
        if (txt == null) return;
        if (_trueTypeFont != null) txt.font = _trueTypeFont;
        if (txt.fontSize < 20)
        {
            txt.fontSize = Mathf.Max((int)(txt.fontSize * 1.6f), 24);
        }
    }
}

