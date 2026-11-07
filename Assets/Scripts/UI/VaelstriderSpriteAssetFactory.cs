using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Runtime TMP Sprite Asset Generator
/// Tự động sinh TextMeshPro Sprite Asset sắc nét tại Runtime chứa các Icon Pixel Art chuẩn LMHT / TFT:
/// - "atk": Thanh kiếm cam (Tấn công vật lý)
/// - "ap": Ngôi sao xanh ngọc (Sức mạnh phép thuật)
/// - "mana": Giọt năng lượng xanh dương (Tiêu hao Mana / Năng lượng)
/// - "hp": Trái tim đỏ (Tiêu hao Máu / Sinh lực)
/// </summary>
public static class VaelstriderSpriteAssetFactory
{
    private static TMP_SpriteAsset cachedAsset;

    public static TMP_SpriteAsset GetOrCreateSpriteAsset()
    {
        if (cachedAsset != null) return cachedAsset;

        int iconSize = 16;
        int iconCount = 4;
        int atlasWidth = iconSize * iconCount;
        int atlasHeight = iconSize;

        Texture2D tex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Khởi tạo nền trong suốt
        Color[] clear = new Color[atlasWidth * atlasHeight];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        // 1. Icon Kiếm Cam (ATK) - Slot 0 (X: 0..15)
        Color swordBlade = new Color(1f, 0.55f, 0.20f, 1f);
        Color swordHi = new Color(1f, 0.88f, 0.50f, 1f);
        Color swordGuard = new Color(0.95f, 0.75f, 0.25f, 1f);
        Color darkOutline = new Color(0.05f, 0.04f, 0.08f, 1f);

        DrawPixelIconToTexture(tex, 0, iconSize, PlayerStatPanelUI.GetPixelIcon("⚔", swordBlade), darkOutline, swordBlade, swordHi);

        // 2. Icon Sao Xanh (AP) - Slot 1 (X: 16..31)
        Color starColor = new Color(0.30f, 0.85f, 1f, 1f);
        Color starHi = new Color(0.80f, 0.96f, 1f, 1f);
        DrawPixelIconToTexture(tex, 1, iconSize, PlayerStatPanelUI.GetPixelIcon("✦", starColor), darkOutline, starColor, starHi);

        // 3. Icon Giọt Mana (MANA) - Slot 2 (X: 32..47)
        Color manaColor = new Color(0.20f, 0.65f, 1f, 1f);
        Color manaHi = new Color(0.70f, 0.90f, 1f, 1f);
        DrawPixelIconToTexture(tex, 2, iconSize, PlayerStatPanelUI.GetPixelIcon("♦", manaColor), darkOutline, manaColor, manaHi);

        // 4. Icon Trái Tim Máu (HP) - Slot 3 (X: 48..63)
        Color hpColor = new Color(1f, 0.30f, 0.35f, 1f);
        Color hpHi = new Color(1f, 0.75f, 0.80f, 1f);
        DrawPixelIconToTexture(tex, 3, iconSize, PlayerStatPanelUI.GetPixelIcon("♥", hpColor), darkOutline, hpColor, hpHi);

        tex.Apply();

        // Tạo ScriptableObject TMP_SpriteAsset
        TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.name = "VaelstriderRuntimeIcons";
        asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);

        Shader shader = Shader.Find("TextMeshPro/Sprite") ?? Shader.Find("UI/Default");
        Material mat = new Material(shader)
        {
            mainTexture = tex
        };
        asset.material = mat;
        asset.spriteSheet = tex;

        string[] names = new[] { "atk", "ap", "mana", "hp" };

        if (asset.spriteGlyphTable != null) asset.spriteGlyphTable.Clear();
        if (asset.spriteCharacterTable != null) asset.spriteCharacterTable.Clear();

        for (int i = 0; i < iconCount; i++)
        {
            int ox = i * iconSize;
            GlyphRect rect = new GlyphRect(ox, 0, iconSize, iconSize);
            GlyphMetrics metrics = new GlyphMetrics(iconSize, iconSize, 0, iconSize * 0.85f, iconSize);

            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph((uint)i, metrics, rect, 1.0f, 0);
            if (asset.spriteGlyphTable != null) asset.spriteGlyphTable.Add(glyph);

            TMP_SpriteCharacter ch = new TMP_SpriteCharacter(0xFFFE, glyph)
            {
                name = names[i],
                scale = 1.0f
            };
            if (asset.spriteCharacterTable != null) asset.spriteCharacterTable.Add(ch);
        }

        asset.UpdateLookupTables();

        cachedAsset = asset;
        return asset;
    }

    private static void DrawPixelIconToTexture(Texture2D atlas, int slot, int size, Sprite srcSprite, Color outline, Color main, Color hi)
    {
        if (srcSprite == null || srcSprite.texture == null) return;
        Texture2D srcTex = srcSprite.texture;

        int startX = slot * size;
        int sw = Mathf.Min(size, srcTex.width);
        int sh = Mathf.Min(size, srcTex.height);
        int ox = (size - sw) / 2;
        int oy = (size - sh) / 2;

        for (int y = 0; y < sh; y++)
        {
            for (int x = 0; x < sw; x++)
            {
                Color c = srcTex.GetPixel(x, y);
                if (c.a > 0.05f)
                {
                    atlas.SetPixel(startX + ox + x, oy + y, c);
                }
            }
        }
    }
}

