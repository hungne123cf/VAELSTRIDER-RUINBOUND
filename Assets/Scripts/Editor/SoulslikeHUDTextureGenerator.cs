using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Soulslike HUD Texture & Sprite Generator
/// Tự động tạo bộ Texture / Sprite cao cấp phong cách Dark Fantasy / Soulslike:
/// - Ruby Crimson Fill (Máu) với dải sáng Specular trên đỉnh & bóng sâu bên dưới
/// - Ethereal Sapphire Fill (Mana/Energy) với ánh dạ quang huyền bí
/// - Amber Brass Fill (Nội năng/Stamina) ánh kim đồng cổ
/// - Amethyst Platinum Fill (EXP) sợi chỉ tím huyền ảo
/// - Obsidian Trough Background (Rãnh đá đen chạm viền chỉ đồng cổ 2 tầng)
/// - Tarnished Bronze Dual Bezel (Vành kim loại mạ đồng cổ hoàng gia chạm rãnh kép cho Avatar)
/// - Runic Stone Plaque (Khay đá Runic viền đồng cổ đựng Linh Hồn / Shards)
/// </summary>
public static class SoulslikeHUDTextureGenerator
{
    private const string FOLDER_PATH = "Assets/Sprites/UI/Soulslike";

    [MenuItem("Vaelstrider/UI/Generate Soulslike HUD Textures")]
    public static void GenerateAllTextures()
    {
        if (!Directory.Exists(FOLDER_PATH))
        {
            Directory.CreateDirectory(FOLDER_PATH);
        }

        CreateRubyHealthFill();
        CreateSapphireEnergyFill();
        CreateAmberStaminaFill();
        CreateAmethystExpFill();
        CreateObsidianTroughBg();
        CreateTarnishedBronzeBezel();
        CreateRunicStonePlaque();

        AssetDatabase.Refresh();
        ConfigureAllSprites();
        Debug.Log("<color=#d4af37><b>[Soulslike HUD]</b> Đã tạo thành công toàn bộ Texture & Sprite phong cách Dark Fantasy / Soulslike!</color>");
    }

    // 1. Thanh Máu Ruby Crimson: Highlight trên đỉnh, thân đỏ ruby rực rỡ, đáy huyết dụ sâu thẳm
    private static void CreateRubyHealthFill()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            Color rowCol;
            if (v > 0.90f)
            {
                // Mép sáng trên đỉnh (Specular highlight)
                rowCol = Color.Lerp(new Color(0.95f, 0.28f, 0.35f, 1f), new Color(1f, 0.65f, 0.70f, 1f), (v - 0.90f) / 0.10f);
            }
            else if (v > 0.45f)
            {
                // Thân giữa đỏ ngọc ruby rực rỡ
                rowCol = Color.Lerp(new Color(0.72f, 0.08f, 0.14f, 1f), new Color(0.92f, 0.18f, 0.24f, 1f), (v - 0.45f) / 0.45f);
            }
            else if (v > 0.10f)
            {
                // Nửa dưới đỏ thẫm huyết dụ
                rowCol = Color.Lerp(new Color(0.38f, 0.04f, 0.08f, 1f), new Color(0.72f, 0.08f, 0.14f, 1f), (v - 0.10f) / 0.35f);
            }
            else
            {
                // Đáy bóng tối kim loại
                rowCol = new Color(0.20f, 0.02f, 0.04f, 1f);
            }

            for (int x = 0; x < w; x++)
            {
                // Bo góc 1px nhẹ 2 bên
                float alpha = 1f;
                if ((x == 0 || x == w - 1) && (y == 0 || y == h - 1)) alpha = 0.5f;
                cols[y * w + x] = new Color(rowCol.r, rowCol.g, rowCol.b, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Bar_Ruby_Fill.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // 2. Thanh Năng Lượng Ethereal Sapphire: Đỉnh lam sáng, thân xanh Sapphire dạ quang, đáy xanh thẫm
    private static void CreateSapphireEnergyFill()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            Color rowCol;
            if (v > 0.88f)
            {
                // Viền sáng dạ quang trên đỉnh
                rowCol = Color.Lerp(new Color(0.35f, 0.78f, 1f, 1f), new Color(0.85f, 0.96f, 1f, 1f), (v - 0.88f) / 0.12f);
            }
            else if (v > 0.40f)
            {
                // Thân xanh lam ngọc rực rỡ
                rowCol = Color.Lerp(new Color(0.10f, 0.42f, 0.75f, 1f), new Color(0.22f, 0.65f, 0.95f, 1f), (v - 0.40f) / 0.48f);
            }
            else
            {
                // Đáy ngọc Lapis Lazuli trầm
                rowCol = Color.Lerp(new Color(0.04f, 0.16f, 0.35f, 1f), new Color(0.10f, 0.42f, 0.75f, 1f), v / 0.40f);
            }

            for (int x = 0; x < w; x++)
            {
                float alpha = 1f;
                if ((x == 0 || x == w - 1) && (y == 0 || y == h - 1)) alpha = 0.5f;
                cols[y * w + x] = new Color(rowCol.r, rowCol.g, rowCol.b, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Bar_Sapphire_Fill.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // 3. Thanh Nội Năng Amber Stamina: Ánh kim đồng / hổ phách cổ đại
    private static void CreateAmberStaminaFill()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            Color rowCol;
            if (v > 0.88f)
            {
                rowCol = Color.Lerp(new Color(0.98f, 0.75f, 0.20f, 1f), new Color(1f, 0.95f, 0.60f, 1f), (v - 0.88f) / 0.12f);
            }
            else if (v > 0.40f)
            {
                rowCol = Color.Lerp(new Color(0.70f, 0.42f, 0.05f, 1f), new Color(0.92f, 0.62f, 0.10f, 1f), (v - 0.40f) / 0.48f);
            }
            else
            {
                rowCol = Color.Lerp(new Color(0.30f, 0.16f, 0.02f, 1f), new Color(0.70f, 0.42f, 0.05f, 1f), v / 0.40f);
            }

            for (int x = 0; x < w; x++)
            {
                float alpha = 1f;
                if ((x == 0 || x == w - 1) && (y == 0 || y == h - 1)) alpha = 0.5f;
                cols[y * w + x] = new Color(rowCol.r, rowCol.g, rowCol.b, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Bar_Amber_Fill.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // 4. Thanh EXP Amethyst: Sợi chỉ thạch anh tím / bạch kim huyền bí
    private static void CreateAmethystExpFill()
    {
        int w = 32, h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            Color rowCol = Color.Lerp(new Color(0.35f, 0.25f, 0.65f, 1f), new Color(0.68f, 0.55f, 0.95f, 1f), v);
            if (v > 0.80f) rowCol = Color.Lerp(rowCol, new Color(0.92f, 0.88f, 1f, 1f), (v - 0.80f) / 0.20f);

            for (int x = 0; x < w; x++)
            {
                cols[y * w + x] = rowCol;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Bar_Amethyst_Fill.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // 5. Rãnh Đá Obsidian Chạm Viền Đồng Cổ (Trough Background)
    private static void CreateObsidianTroughBg()
    {
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color bronzeTopBorder = new Color(0.72f, 0.55f, 0.28f, 0.95f); // Chỉ đồng cổ ánh sáng
        Color bronzeBottomBorder = new Color(0.38f, 0.28f, 0.14f, 0.90f); // Chỉ đồng cổ bóng chìm
        Color obsidianBody = new Color(0.06f, 0.07f, 0.10f, 0.96f); // Lòng rãnh đá đen

        for (int y = 0; y < h; y++)
        {
            Color rowCol;
            if (y == h - 1)
            {
                rowCol = bronzeTopBorder; // Mép trên cùng
            }
            else if (y == h - 2)
            {
                rowCol = Color.Lerp(bronzeTopBorder, obsidianBody, 0.6f);
            }
            else if (y == 0)
            {
                rowCol = bronzeBottomBorder; // Mép dưới cùng
            }
            else if (y == 1)
            {
                rowCol = Color.Lerp(bronzeBottomBorder, obsidianBody, 0.6f);
            }
            else
            {
                // Gradient sâu nhẹ ở lòng rãnh
                float t = (float)y / (h - 1);
                rowCol = Color.Lerp(new Color(0.04f, 0.05f, 0.08f, 0.98f), obsidianBody, Mathf.Sin(t * Mathf.PI));
            }

            for (int x = 0; x < w; x++)
            {
                Color c = rowCol;
                // Viền đồng 2 đầu hồi
                if (x == 0 || x == w - 1)
                {
                    c = Color.Lerp(c, bronzeTopBorder, 0.5f);
                }
                cols[y * w + x] = c;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Bar_Obsidian_Trough.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // 6. Vành Bezel Mạ Đồng Cổ Rãnh Kép Cho Avatar (Tarnished Bronze Dual Bezel)
    private static void CreateTarnishedBronzeBezel()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        float center = (size - 1) * 0.5f;
        float outerR = center - 0.5f;      // ~63.0
        float grooveR = outerR - 4.5f;     // ~58.5
        float innerR = grooveR - 5.5f;     // ~53.0

        Color goldLight = new Color(0.88f, 0.72f, 0.38f, 1f);  // Vàng đồng ánh sáng
        Color goldDark = new Color(0.48f, 0.35f, 0.16f, 1f);   // Vàng đồng đổ bóng
        Color grooveCol = new Color(0.12f, 0.09f, 0.05f, 1f);  // Rãnh khắc sâu đen

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > outerR + 0.5f || dist < innerR - 0.5f)
                {
                    cols[y * size + x] = Color.clear;
                    continue;
                }

                // Góc ánh sáng từ hướng 10 giờ (Tây Bắc) rọi xuống 4 giờ (Đông Nam)
                float angle = Mathf.Atan2(y - center, x - center); // rad [-pi, pi]
                float lightFactor = (Mathf.Sin(angle + 0.75f) + 1f) * 0.5f; // [0, 1]
                Color metalTone = Color.Lerp(goldDark, goldLight, lightFactor);

                Color pixelCol;
                if (Mathf.Abs(dist - grooveR) < 1.2f)
                {
                    // Rãnh khắc sâu giữa 2 vành kim loại
                    pixelCol = grooveCol;
                }
                else if (dist > grooveR)
                {
                    // Vành ngoài chạm nổi
                    float edgeDist = Mathf.Min(outerR - dist, dist - grooveR);
                    float edgeBlend = Mathf.Clamp01(edgeDist + 0.5f);
                    pixelCol = Color.Lerp(metalTone * 0.7f, metalTone, edgeBlend);
                }
                else
                {
                    // Vành trong mạ vàng cổ
                    float edgeDist = Mathf.Min(grooveR - dist, dist - innerR);
                    float edgeBlend = Mathf.Clamp01(edgeDist + 0.5f);
                    pixelCol = Color.Lerp(metalTone * 0.8f, metalTone * 1.1f, edgeBlend);
                }

                // Khử răng cưa mép ngoài và mép trong
                float alphaOuter = Mathf.Clamp01(outerR - dist + 0.5f);
                float alphaInner = Mathf.Clamp01(dist - innerR + 0.5f);
                float finalAlpha = Mathf.Min(alphaOuter, alphaInner);

                cols[y * size + x] = new Color(pixelCol.r, pixelCol.g, pixelCol.b, finalAlpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Avatar_Bronze_Bezel.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // 7. Khay Đá Runic Chứa Linh Hồn / Aethel Shard (Runic Stone Cartouche)
    private static void CreateRunicStonePlaque()
    {
        int w = 64, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color stoneBody = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        Color bronzeBorder = new Color(0.68f, 0.52f, 0.25f, 0.95f);
        Color innerGroove = new Color(0.02f, 0.03f, 0.05f, 0.98f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Bo góc 3px
                bool isCorner = (x < 3 || x >= w - 3) && (y < 3 || y >= h - 3);
                bool isExtremeCorner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);

                if (isExtremeCorner)
                {
                    cols[y * w + x] = Color.clear;
                    continue;
                }

                bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);
                bool isGroove = (x == 2 || x == w - 3 || y == 2 || y == h - 3);

                if (isBorder)
                {
                    cols[y * w + x] = bronzeBorder;
                }
                else if (isGroove)
                {
                    cols[y * w + x] = innerGroove;
                }
                else
                {
                    cols[y * w + x] = stoneBody;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(FOLDER_PATH, "Runic_Plaque_Bg.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureAllSprites()
    {
        string[] files = Directory.GetFiles(FOLDER_PATH, "*.png");
        foreach (string file in files)
        {
            string unityPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                string filename = Path.GetFileName(file);
                if (filename.Contains("Bar_") || filename.Contains("Runic_"))
                {
                    // 9-slice border (L: 4, B: 4, R: 4, T: 4)
                    importer.spriteBorder = new Vector4(6, 4, 6, 4);
                }

                importer.SaveAndReimport();
            }
        }
    }
}
