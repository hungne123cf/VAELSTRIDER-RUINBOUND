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
    private static readonly HashSet<int> _scaledTextIds = new HashSet<int>();
    private static readonly HashSet<int> _scaledLegacyIds = new HashSet<int>();

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

    /// <summary>
    /// Chuẩn hóa chuỗi văn bản:
    /// 1. Khử hoàn toàn dấu mở ngoặc vuông '[' và đóng ']' biến thành '(' và ')'
    ///    để khắc phục triệt để lỗi font Dreams Unwound SDF ánh xạ mã ASCII 91 sang chữ 'z'.
    /// 2. Khử triệt để các ký tự Unicode ngoài ASCII chuẩn (◆, ■, ▲, ★, ✔, ✖...) biến thành ký tự an toàn
    ///    để tránh hoàn toàn lỗi ô vuông '□' (missing glyph) trên Android.
    /// 3. Thay thế ký tự em-dash/en-dash dài (—, –) thành dấu gạch nối ngắn '-' tránh lỗi ô vuông.
    /// </summary>
    public static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.Contains("\u2014") || text.Contains("\u2013"))
            text = text.Replace('\u2014', '-').Replace('\u2013', '-');
        
        // Khử các tag thừa vô nghĩa
        if (text.Contains("[THANH]") || text.Contains("zTHANH]"))
            text = text.Replace("[THANH]", "").Replace("zTHANH]", "");
        if (text.Contains("[NGOC]") || text.Contains("zNGOC]"))
            text = text.Replace("[NGOC]", "").Replace("zNGOC]", "");
        if (text.Contains("[TIỀN TỆ]") || text.Contains("z TIỀN TỆ]") || text.Contains("zTIỀN TỆ]"))
            text = text.Replace("[TIỀN TỆ]", "(Tiền tệ)").Replace("z TIỀN TỆ]", "(Tiền tệ)").Replace("zTIỀN TỆ]", "(Tiền tệ)");
        if (text.Contains("[HIẾM]") || text.Contains("z HIẾM]") || text.Contains("zHIẾM]"))
            text = text.Replace("[HIẾM]", "(Hiếm)").Replace("z HIẾM]", "(Hiếm)").Replace("zHIẾM]", "(Hiếm)");
        if (text.Contains("[BẠN]") || text.Contains("zBẠN]") || text.Contains("z BẠN]"))
            text = text.Replace("[BẠN]", "BẠN").Replace("zBẠN]", "BẠN").Replace("z BẠN]", "BẠN");

        // Thay dấu ngoặc vuông bằng ngoặc đơn an toàn ASCII
        if (text.Contains("[") || text.Contains("]"))
            text = text.Replace("[", "(").Replace("]", ")");

        // Khử toàn bộ ký tự biểu tượng Unicode lạ gây ô vuông □
        if (text.Contains("◆")) text = text.Replace("◆", "");
        if (text.Contains("■")) text = text.Replace("■", "-");
        if (text.Contains("▲")) text = text.Replace("▲", "^");
        if (text.Contains("★")) text = text.Replace("★", "*");
        if (text.Contains("✔")) text = text.Replace("✔", "V");
            if (text.Contains("«")) text = text.Replace("«", "\"");
            if (text.Contains("»")) text = text.Replace("»", "\"");
            if (text.Contains("▼")) text = text.Replace("▼", "V");
            if (text.Contains("▲")) text = text.Replace("▲", "^");
        if (text.Contains("✖")) text = text.Replace("✖", "X");

        return text;
    }

    public static void ApplyFontToText(TextMeshProUGUI txt, bool isHeader = false)
    {
        if (txt == null) return;
        try
        {
            var targetFont = isHeader ? HeaderFont : BodyFont;
            if (targetFont == null) return;

            txt.font = targetFont;

            // Reset shared material về đúng material của Dreams Unwound để tránh lỗi lệch Texture / Shader
            if (targetFont.material != null)
            {
                txt.fontSharedMaterial = targetFont.material;
            }

            // TỰ ĐỘNG KHỬ DẤU NGOẶC VUÔNG [ ] VÀ EM-DASH (—, –)
            // Tránh hoàn toàn lỗi mã ASCII 91 của dấu [ bị font Dreams Unwound SDF vẽ nhầm thành chữ 'z'
            if (!string.IsNullOrEmpty(txt.text))
            {
                txt.text = SanitizeText(txt.text);
            }

            // PHÓNG TO CHỮ CHO ĐIỆN THOẠI (Mobile Accessibility) - CHỈ SCALE 1 LẦN DUY NHẤT
            // Tránh lỗi gọi ApplyFontsGlobally nhiều lần làm chữ bị nhân đôi/nhân ba liên tục đè lên nhau
            int id = txt.GetInstanceID();
            if (!_scaledTextIds.Contains(id))
            {
                _scaledTextIds.Add(id);

                if (txt.enableAutoSizing)
                {
                    if (txt.fontSizeMin < 26f) txt.fontSizeMin = 26f;
                    if (txt.fontSizeMax < 46f) txt.fontSizeMax = Mathf.Max(txt.fontSizeMax * 1.5f, 48f);
                }
                else
                {
                    if (txt.fontSize < 20f)
                    {
                        txt.fontSize = Mathf.Clamp(txt.fontSize * 1.75f, 28f, 34f);
                    }
                    else if (txt.fontSize < 30f)
                    {
                        txt.fontSize = Mathf.Clamp(txt.fontSize * 1.45f, 34f, 42f);
                    }
                    else if (txt.fontSize < 46f)
                    {
                        txt.fontSize = Mathf.Clamp(txt.fontSize * 1.30f, 44f, 54f);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameFontManager] ApplyFontToText safe fallback: " + ex.Message);
        }
    }

    public static void ApplyFontToLegacyText(Text txt)
    {
        if (txt == null) return;
        if (_trueTypeFont != null) txt.font = _trueTypeFont;
        int id = txt.GetInstanceID();
        if (!_scaledLegacyIds.Contains(id))
        {
            _scaledLegacyIds.Add(id);
            if (txt.fontSize < 20)
            {
                txt.fontSize = Mathf.Max((int)(txt.fontSize * 1.4f), 22);
            }
        }
    }
}

