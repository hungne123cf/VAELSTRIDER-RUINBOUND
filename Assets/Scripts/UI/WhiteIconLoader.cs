// ========================================================
// VAELSTRIDER: RUINBOUND - White Icon Asset Manager & UI Utility
// Tự động tải và gán toàn bộ hệ thống icon tinh giản (Gear, Cross, Cart, Home, Play, Pause, Info, Language...)
// từ Assets/ART/White-Icon vào tất cả các nút bấm & giao diện tương ứng!
// ========================================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class WhiteIconLoader
{
    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Lấy Sprite từ thư mục Assets/ART/White-Icon/ theo tên hoặc alias
    /// </summary>
    public static Sprite GetSprite(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // Chuẩn hoá alias
        string standardName = ResolveAlias(name.Trim());

        if (_cache.TryGetValue(standardName, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite spr = null;

        // 1. Thử load trong Editor qua AssetDatabase
#if UNITY_EDITOR
        string assetPath = $"Assets/ART/White-Icon/{standardName}.png";
        spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (spr == null)
        {
            spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/White-Icon/{name}.png");
        }
#endif

        // 2. Thử load qua Resources
        if (spr == null)
        {
            spr = Resources.Load<Sprite>($"White-Icon/{standardName}");
            if (spr == null) spr = Resources.Load<Sprite>(standardName);
        }

        // 3. Fallback nạp trực tiếp qua File System (Hoạt động cả khi chạy Standalone / Playmode)
        if (spr == null)
        {
            string fileName = standardName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? standardName : standardName + ".png";
            string diskPath = Path.Combine(Application.dataPath, "ART/White-Icon", fileName);
            if (!File.Exists(diskPath))
            {
                diskPath = Path.Combine(Application.dataPath, "ART/White-Icon", name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? name : name + ".png");
            }

            if (File.Exists(diskPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(diskPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WhiteIconLoader] Không thể tải icon từ disk {diskPath}: {ex.Message}");
                }
            }
        }

        if (spr != null)
        {
            _cache[standardName] = spr;
        }

        return spr;
    }

    private static string ResolveAlias(string name)
    {
        string lower = name.ToLower().Replace(" ", "").Replace("_", "-");
        if (lower.Contains("gear") || lower == "opt" || lower == "setting" || lower == "settings" || lower == "option" || lower == "options" || lower == "cai-dat")
            return "Gear";
        if (lower.Contains("cross") || lower == "close" || lower == "x" || lower == "cancel" || lower == "dong" || lower == "exit-modal")
            return "Cross";
        if (lower.Contains("home") || lower == "mainmenu" || lower == "menu" || lower == "lobby" || lower == "trang-chu")
            return "Home";
        if (lower.Contains("cart") || lower == "shop" || lower == "store" || lower == "cua-hang" || lower == "tiem")
            return "Cart";
        if (lower == "exit" || lower == "logout" || lower == "quit" || lower == "dang-xuat" || lower == "thoat")
            return "Exit";
        if (lower == "play" || lower == "battle" || lower == "start" || lower == "xuat-tran" || lower == "tiep-tuc")
            return "Play";
        if (lower == "pause" || lower == "tam-dung")
            return "Pause";
        if (lower.Contains("info") || lower == "stats" || lower == "thong-tin" || lower == "guide" || lower == "about")
            return "Info";
        if (lower.Contains("language") || lower.Contains("globe") || lower == "lang" || lower == "ngon-ngu")
            return "Language_Globe";
        if (lower.Contains("sound-none") || lower == "mute" || lower == "sound-off" || lower == "tat-tieng")
            return "Sound-None";
        if (lower.Contains("sound-three") || lower == "sound" || lower == "sfx" || lower == "volume" || lower == "am-thanh")
            return "Sound-Three";
        if (lower.Contains("music-off") || lower == "bgm-off" || lower == "tat-nhac")
            return "Music-Off";
        if (lower.Contains("music-on") || lower == "music" || lower == "bgm" || lower == "am-nhac")
            return "Music-On";
        if (lower.Contains("repeat") || lower.Contains("restart") || lower == "replay" || lower == "choi-lai")
            return "Repeat-Right";
        if (lower.Contains("hero") || lower == "roster" || lower == "nhan-vat" || lower == "anh-hung")
            return "Hero";
        if (lower.Contains("player"))
            return "Player";
        if (lower.Contains("level") || lower == "levels" || lower == "stage" || lower == "map" || lower == "man-choi")
            return "Levels";
        if (lower.Contains("star") || lower == "skill" || lower == "favourite" || lower == "ngoc")
            return "Star";
        if (lower.Contains("tick") || lower == "check" || lower == "confirm" || lower == "ok" || lower == "xac-nhan")
            return "Tick";
        if (lower.Contains("bag") || lower == "inventory" || lower == "tui-do" || lower == "kho")
            return "Bag";
        if (lower.Contains("lock-unlocked"))
            return "Locker-Unlocked";
        if (lower.Contains("lock-locked") || lower == "lock" || lower == "khoa")
            return "Locker-Locked";
        if (lower.Contains("arrow-left") || lower == "back" || lower == "quay-lai")
            return "SolidArrow-Left";
        if (lower.Contains("arrow-right") || lower == "next" || lower == "ke-tiep")
            return "SolidArrow-Right";

        return name;
    }

    /// <summary>
    /// Thay thế nội dung nút (như text cũ "OPT", "X", "BAG", "INFO") bằng Icon tương ứng
    /// </summary>
    public static Image ReplaceTextWithIcon(Button btn, string iconName, Color? iconColor = null, Vector2? iconSize = null)
    {
        if (btn == null) return null;

        // Ẩn tất cả text tạm trong button
        var tmpTexts = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmpTexts)
        {
            t.gameObject.SetActive(false);
        }
        var legacyTexts = btn.GetComponentsInChildren<Text>(true);
        foreach (var t in legacyTexts)
        {
            t.gameObject.SetActive(false);
        }

        // Tạo hoặc tìm Image Icon
        Transform iconT = btn.transform.Find("WhiteIcon");
        GameObject iconObj;
        if (iconT != null)
        {
            iconObj = iconT.gameObject;
        }
        else
        {
            iconObj = new GameObject("WhiteIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.layer = 5;
            iconObj.transform.SetParent(btn.transform, false);
        }

        iconObj.SetActive(true);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        
        Vector2 finalSize = iconSize ?? new Vector2(26f, 26f);
        iconRt.sizeDelta = finalSize;

        Image img = iconObj.GetComponent<Image>();
        img.sprite = GetSprite(iconName);
        img.preserveAspect = true;
        img.color = iconColor ?? new Color(1f, 0.88f, 0.45f, 1f); // Màu vàng gold hoàng gia mặc định
        img.raycastTarget = false;

        return img;
    }

    /// <summary>
    /// Gán Icon bên cạnh hoặc phía trên Text của một Button có cả icon và nhãn chữ
    /// </summary>
    public static Image SetButtonIconAndText(Button btn, string iconName, string text, Color? iconColor = null, Color? textColor = null, bool iconOnLeft = true)
    {
        if (btn == null) return null;

        // 1. Cập nhật Text
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.gameObject.SetActive(true);
            tmp.text = text;
            if (textColor.HasValue) tmp.color = textColor.Value;

            RectTransform txtRt = tmp.GetComponent<RectTransform>();
            if (iconOnLeft)
            {
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(50f, 0f);
                txtRt.offsetMax = new Vector2(-16f, 0f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 13f;
                tmp.fontSizeMax = 17f;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
            }
            else
            {
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        // 2. Cập nhật Icon
        Transform iconT = btn.transform.Find("WhiteIcon");
        GameObject iconObj;
        if (iconT != null)
        {
            iconObj = iconT.gameObject;
        }
        else
        {
            iconObj = new GameObject("WhiteIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.layer = 5;
            iconObj.transform.SetParent(btn.transform, false);
        }

        iconObj.SetActive(true);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        if (iconOnLeft)
        {
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(24f, 0f);
            iconRt.sizeDelta = new Vector2(22f, 22f);
        }
        else
        {
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(26f, 26f);
        }

        Image img = iconObj.GetComponent<Image>();
        img.sprite = GetSprite(iconName);
        img.preserveAspect = true;
        img.color = iconColor ?? Color.white;
        img.raycastTarget = false;

        return img;
    }

    private static Sprite _frameBorderSprite;

    /// <summary>
    /// <summary>
    /// Lấy Sprite khung viền Gothic (frame_gothic_border.png 9-slice rỗng ở giữa) dùng làm viền nổi cho các panel/card
    /// </summary>
    public static Sprite GetFrameBorderSprite()
    {
        if (_frameBorderSprite != null) return _frameBorderSprite;

        // Ưu tiên tải viền rỗng ở giữa (frame_gothic_border.png) để không che tối nội dung bên dưới
        _frameBorderSprite = Resources.Load<Sprite>("UI/frame_gothic_border");
        if (_frameBorderSprite != null) return _frameBorderSprite;

#if UNITY_EDITOR
        _frameBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/frame_gothic_border.png");
        if (_frameBorderSprite != null) return _frameBorderSprite;
#endif

        string[] candidates = new string[]
        {
            Path.Combine(Application.dataPath, "Sprites/UI/frame_gothic_border.png"),
            Path.Combine(Application.dataPath, "Resources/UI/frame_gothic_border.png"),
            Path.Combine(Application.dataPath, "ART/UI/Default/Border/panel-border-004.png"),
            Path.Combine(Application.dataPath, "Sprites/UI/Soulslike/Dark_Fantasy_Panel_Bg.png")
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        _frameBorderSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
                        break;
                    }
                }
                catch {}
            }
        }

        return _frameBorderSprite;
    }

    private static Sprite _dividerFadeSprite;

    /// <summary>
    /// Lấy Sprite thanh divider cổ kính Gothic Filigree (Gothic_Filigree_Divider.png)
    /// </summary>
    public static Sprite GetDividerFadeSprite()
    {
        if (_dividerFadeSprite != null) return _dividerFadeSprite;

        Sprite soulslikeDiv = SoulslikeHUDStyler.GetGothicDividerSprite();
        if (soulslikeDiv != null)
        {
            _dividerFadeSprite = soulslikeDiv;
            return _dividerFadeSprite;
        }

#if UNITY_EDITOR
        _dividerFadeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/divider_fade_004.png");
        if (_dividerFadeSprite != null) return _dividerFadeSprite;
#endif

        string[] candidates = new string[]
        {
            Path.Combine(Application.dataPath, "Sprites/UI/Soulslike/Gothic_Filigree_Divider.png"),
            Path.Combine(Application.dataPath, "Resources/UI/Soulslike/Gothic_Filigree_Divider.png"),
            Path.Combine(Application.dataPath, "Sprites/UI/divider_fade_004.png"),
            Path.Combine(Application.dataPath, "ART/UI/Default/Divider Fade/divider-fade-004.png"),
            Path.Combine(Application.dataPath, "Resources/UI/divider_fade_004.png")
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        _dividerFadeSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 0, 24, 0));
                        break;
                    }
                }
                catch {}
            }
        }

        return _dividerFadeSprite;
    }

    private static Sprite _dividerFade000Sprite;

    /// <summary>
    /// Lấy Sprite thanh divider-fade-000 (Assets/ART/UI/Default/Divider Fade/divider-fade-000.png)
    /// </summary>
    public static Sprite GetDividerFade000Sprite()
    {
        if (_dividerFade000Sprite != null) return _dividerFade000Sprite;

#if UNITY_EDITOR
        _dividerFade000Sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/UI/Default/Divider Fade/divider-fade-000.png");
        if (_dividerFade000Sprite != null) return _dividerFade000Sprite;
#endif

        string[] candidates = new string[]
        {
            Path.Combine(Application.dataPath, "ART/UI/Default/Divider Fade/divider-fade-000.png"),
            Path.Combine(Application.dataPath, "Sprites/UI/divider-fade-000.png"),
            Path.Combine(Application.dataPath, "Resources/UI/divider-fade-000.png")
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        _dividerFade000Sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), 100f);
                        return _dividerFade000Sprite;
                    }
                }
                catch {}
            }
        }

        return null;
    }

    private static Sprite _gothicBracketSprite;

    /// <summary>
    /// Lấy Sprite hoa văn ngoặc kép Gothic (gothic_bracket.png)
    /// </summary>
    public static Sprite GetGothicBracketSprite()
    {
        if (_gothicBracketSprite != null) return _gothicBracketSprite;

#if UNITY_EDITOR
        _gothicBracketSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/gothic_bracket.png");
        if (_gothicBracketSprite != null) return _gothicBracketSprite;
#endif

        string[] candidates = new string[]
        {
            Path.Combine(Application.dataPath, "Sprites/UI/gothic_bracket.png"),
            Path.Combine(Application.dataPath, "Resources/UI/gothic_bracket.png")
        };

        foreach (var p in candidates)
        {
            if (File.Exists(p))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        _gothicBracketSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), 100f);
                        return _gothicBracketSprite;
                    }
                }
                catch {}
            }
        }

        // Fallback: cắt phần hoa văn 22x14 ở góc phải của divider_fade_004
        Sprite divSpr = GetDividerFadeSprite();
        if (divSpr != null && divSpr.texture != null)
        {
            try
            {
                _gothicBracketSprite = Sprite.Create(divSpr.texture, new Rect(72f, 0f, 22f, 14f), new Vector2(0.5f, 0.5f), 100f);
                return _gothicBracketSprite;
            }
            catch {}
        }

        return null;
    }


    /// <summary>
    /// Gắn viền Gothic 9-slice thanh lịch lên bất kỳ RectTransform/Panel nào, thay thế outline màu chói
    /// </summary>
    public static Image ApplyGothicFrame(GameObject targetObj, Color? frameColor = null, float pixelsMultiplier = 0.45f)
    {
        if (targetObj == null) return null;

        // Xóa Outline thô cũ nếu có
        var outlines = targetObj.GetComponents<Outline>();
        foreach (var o in outlines)
        {
            o.enabled = false;
        }

        Transform existingFrame = targetObj.transform.Find("GothicFrameBorder");
        GameObject frameObj;
        if (existingFrame != null)
        {
            frameObj = existingFrame.gameObject;
        }
        else
        {
            frameObj = new GameObject("GothicFrameBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameObj.layer = 5;
            frameObj.transform.SetParent(targetObj.transform, false);
            frameObj.transform.SetAsLastSibling();
        }

        RectTransform frt = frameObj.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        Image img = frameObj.GetComponent<Image>();
        img.sprite = GetFrameBorderSprite();
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = pixelsMultiplier;
        img.color = frameColor ?? Color.white;
        img.raycastTarget = false;

        return img;
    }
}
