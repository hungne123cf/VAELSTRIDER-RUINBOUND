using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class ItemSpriteGenerator
{
    static ItemSpriteGenerator()
    {
        EditorApplication.delayCall += GenerateMissingItemSprites;
    }

    [MenuItem("Tools/Generate Item Sprites")]
    public static void GenerateMissingItemSprites()
    {
        string spriteDir = "Assets/Sprites/Items";
        if (!Directory.Exists(spriteDir)) Directory.CreateDirectory(spriteDir);

        GenerateScrollSprite(Path.Combine(spriteDir, "scroll_summon_mythic.png"));
        GenerateCoreSoulSprite(Path.Combine(spriteDir, "core_abyssal_soul.png"));
        GenerateKeySprite(Path.Combine(spriteDir, "key_ancient_dungeon.png"));
        GenerateFuryPotionSprite(Path.Combine(spriteDir, "potion_fury_elixir.png"));
        GenerateWindPotionSprite(Path.Combine(spriteDir, "potion_windstride.png"));

        AssetDatabase.Refresh();
    }

    private static void SaveAndConfigureSprite(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    // 1. Ancient Mystic Summon Scroll (Bùa Triệu Hồi Thần Bí)
    private static void GenerateScrollSprite(string path)
    {
        if (File.Exists(path)) return;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        Color parchment = new Color(0.92f, 0.82f, 0.60f, 1f);
        Color parchmentShadow = new Color(0.72f, 0.58f, 0.38f, 1f);
        Color rollWood = new Color(0.48f, 0.28f, 0.12f, 1f);
        Color rollGold = new Color(1.0f, 0.85f, 0.25f, 1f);
        Color sealRed = new Color(0.92f, 0.15f, 0.22f, 1f);
        Color sealGlow = new Color(1.0f, 0.45f, 0.55f, 1f);
        Color runeCyan = new Color(0.15f, 0.85f, 1.0f, 1f);
        Color outline = new Color(0.12f, 0.08f, 0.05f, 1f);

        // Body of scroll (diagonal parchment)
        for (int y = 6; y <= 25; y++)
        {
            for (int x = 8; x <= 23; x++)
            {
                if (x == 8 || x == 23 || y == 6 || y == 25)
                    tex.SetPixel(x, y, outline);
                else if (x == 9 || y == 7)
                    tex.SetPixel(x, y, parchmentShadow);
                else
                    tex.SetPixel(x, y, parchment);
            }
        }

        // Top roll bar
        for (int x = 6; x <= 25; x++)
        {
            tex.SetPixel(x, 26, rollWood);
            tex.SetPixel(x, 27, (x == 6 || x == 25) ? rollGold : rollWood);
            tex.SetPixel(x, 28, outline);
        }

        // Bottom roll bar
        for (int x = 6; x <= 25; x++)
        {
            tex.SetPixel(x, 5, outline);
            tex.SetPixel(x, 4, rollWood);
            tex.SetPixel(x, 3, (x == 6 || x == 25) ? rollGold : outline);
        }

        // Mystic Seal & Runes
        for (int dy = -3; dy <= 3; dy++)
        {
            for (int dx = -3; dx <= 3; dx++)
            {
                if (dx * dx + dy * dy <= 8)
                {
                    tex.SetPixel(15 + dx, 15 + dy, sealRed);
                }
            }
        }
        tex.SetPixel(15, 15, sealGlow);
        tex.SetPixel(11, 20, runeCyan);
        tex.SetPixel(12, 19, runeCyan);
        tex.SetPixel(19, 11, runeCyan);
        tex.SetPixel(20, 10, runeCyan);

        tex.Apply();
        SaveAndConfigureSprite(tex, path);
    }

    // 2. Abyssal Soul Core (Lõi Linh Hồn Vực Thẳm)
    private static void GenerateCoreSoulSprite(string path)
    {
        if (File.Exists(path)) return;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        Vector2 center = new Vector2(15.5f, 15.5f);
        Color coreWhite = new Color(1.0f, 0.95f, 1.0f, 1.0f);
        Color corePink = new Color(1.0f, 0.25f, 0.75f, 1.0f);
        Color corePurple = new Color(0.65f, 0.12f, 0.90f, 1.0f);
        Color coreDark = new Color(0.20f, 0.05f, 0.35f, 1.0f);
        Color cosmicCyan = new Color(0.2f, 0.95f, 1.0f, 1.0f);
        Color ringGold = new Color(1.0f, 0.85f, 0.25f, 1.0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= 10.5f)
                {
                    if (dist < 2.5f) tex.SetPixel(x, y, coreWhite);
                    else if (dist < 5.0f) tex.SetPixel(x, y, corePink);
                    else if (dist < 8.0f) tex.SetPixel(x, y, corePurple);
                    else if (dist < 10.0f) tex.SetPixel(x, y, coreDark);
                    else tex.SetPixel(x, y, new Color(0.1f, 0f, 0.15f, 1f));
                }
            }
        }

        // Orbiting Cosmic Energy Sparks
        tex.SetPixel(15, 27, cosmicCyan);
        tex.SetPixel(16, 27, cosmicCyan);
        tex.SetPixel(27, 15, cosmicCyan);
        tex.SetPixel(15, 4, ringGold);
        tex.SetPixel(4, 15, ringGold);
        tex.SetPixel(7, 23, cosmicCyan);
        tex.SetPixel(24, 7, ringGold);

        tex.Apply();
        SaveAndConfigureSprite(tex, path);
    }

    // 3. Ancient Dungeon Key (Chìa Khóa Tàn Tích Cổ)
    private static void GenerateKeySprite(string path)
    {
        if (File.Exists(path)) return;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        Color goldBright = new Color(1.0f, 0.92f, 0.35f, 1f);
        Color goldBase = new Color(0.90f, 0.72f, 0.15f, 1f);
        Color goldShadow = new Color(0.60f, 0.42f, 0.08f, 1f);
        Color gemRuby = new Color(0.95f, 0.15f, 0.25f, 1f);
        Color outline = new Color(0.18f, 0.12f, 0.04f, 1f);

        // Key bow (head ring)
        for (int y = 18; y <= 28; y++)
        {
            for (int x = 11; x <= 21; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(16, 23));
                if (d <= 5.2f && d >= 2.2f)
                {
                    tex.SetPixel(x, y, (x <= 16) ? goldBright : goldBase);
                }
            }
        }
        // Ruby embedded in head
        tex.SetPixel(16, 23, gemRuby);

        // Key shaft (blade)
        for (int y = 6; y <= 18; y++)
        {
            tex.SetPixel(15, y, goldBright);
            tex.SetPixel(16, y, goldBase);
            tex.SetPixel(17, y, goldShadow);
        }

        // Key bit (teeth)
        for (int x = 18; x <= 22; x++)
        {
            tex.SetPixel(x, 7, goldBase);
            tex.SetPixel(x, 8, goldBright);
            tex.SetPixel(x, 11, goldBase);
            tex.SetPixel(x, 12, goldBright);
        }

        tex.Apply();
        SaveAndConfigureSprite(tex, path);
    }

    // 4. Fury Elixir Potion (Dược Liệu Cuồng Nộ)
    private static void GenerateFuryPotionSprite(string path)
    {
        if (File.Exists(path)) return;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        Color cork = new Color(0.55f, 0.35f, 0.18f, 1f);
        Color glass = new Color(0.85f, 0.92f, 0.98f, 0.75f);
        Color liquidFlame = new Color(1.0f, 0.35f, 0.05f, 1f);
        Color liquidYellow = new Color(1.0f, 0.85f, 0.20f, 1f);
        Color liquidDark = new Color(0.70f, 0.12f, 0.05f, 1f);
        Color outline = new Color(0.10f, 0.08f, 0.12f, 1f);

        // Cork
        for (int x = 14; x <= 17; x++)
        {
            tex.SetPixel(x, 26, cork);
            tex.SetPixel(x, 25, cork);
        }
        // Neck
        for (int y = 21; y <= 24; y++)
        {
            tex.SetPixel(13, y, outline);
            tex.SetPixel(14, y, glass);
            tex.SetPixel(17, y, glass);
            tex.SetPixel(18, y, outline);
        }

        // Flask bulb
        for (int y = 5; y <= 20; y++)
        {
            for (int x = 8; x <= 23; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 13f));
                if (d <= 7.8f)
                {
                    if (d > 6.8f)
                    {
                        tex.SetPixel(x, y, outline);
                    }
                    else if (y <= 16)
                    {
                        if (x == 12 && y == 14) tex.SetPixel(x, y, liquidYellow);
                        else if (x <= 13) tex.SetPixel(x, y, liquidFlame);
                        else tex.SetPixel(x, y, liquidDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, (x == 11 || x == 12) ? glass : transparent);
                    }
                }
            }
        }

        tex.Apply();
        SaveAndConfigureSprite(tex, path);
    }

    // 5. Windstride Draught Potion (Linh Dược Tốc Biến)
    private static void GenerateWindPotionSprite(string path)
    {
        if (File.Exists(path)) return;
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, transparent);

        Color cork = new Color(0.35f, 0.45f, 0.65f, 1f);
        Color glass = new Color(0.85f, 0.98f, 1.0f, 0.8f);
        Color windCyan = new Color(0.15f, 0.95f, 1.0f, 1f);
        Color windTeal = new Color(0.05f, 0.65f, 0.75f, 1f);
        Color windWhite = new Color(0.85f, 1.0f, 1.0f, 1f);
        Color outline = new Color(0.05f, 0.12f, 0.18f, 1f);

        // Cork with crystal top
        for (int x = 14; x <= 17; x++)
        {
            tex.SetPixel(x, 26, cork);
            tex.SetPixel(x, 25, cork);
        }
        tex.SetPixel(15, 27, windCyan);
        tex.SetPixel(16, 27, windCyan);

        // Triangular Alchemical Flask
        for (int y = 5; y <= 24; y++)
        {
            int halfW = Mathf.RoundToInt((24 - y) * 0.48f) + 2;
            int startX = 15 - halfW;
            int endX = 16 + halfW;
            for (int x = startX; x <= endX; x++)
            {
                if (x == startX || x == endX || y == 5 || y == 24)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (y <= 17)
                {
                    if ((x == 14 && y == 11) || (x == 15 && y == 12))
                        tex.SetPixel(x, y, windWhite);
                    else if (x <= 15)
                        tex.SetPixel(x, y, windCyan);
                    else
                        tex.SetPixel(x, y, windTeal);
                }
                else
                {
                    tex.SetPixel(x, y, (x == startX + 1) ? glass : transparent);
                }
            }
        }

        tex.Apply();
        SaveAndConfigureSprite(tex, path);
    }
}
