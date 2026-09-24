using UnityEngine;
using System.IO;

public static class SpiderWebSpriteFactory
{
    private static Sprite[] bulletSprites;
    private static Sprite[] trapSprites;
    private static Sprite[] breakSprites;

    public static Sprite[] GetBulletSprites()
    {
        if (bulletSprites != null && bulletSprites.Length == 4 && bulletSprites[0] != null)
            return bulletSprites;

        bulletSprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            bulletSprites[i] = LoadOrGenerateSprite($"Assets/ART/SpiderWeb/SpiderWeb_Bullet_{i}.png", 32, 32, 16f, true, i);
        }
        return bulletSprites;
    }

    public static Sprite[] GetTrapSprites()
    {
        if (trapSprites != null && trapSprites.Length == 4 && trapSprites[0] != null)
            return trapSprites;

        trapSprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            trapSprites[i] = LoadOrGenerateSprite($"Assets/ART/SpiderWeb/SpiderWeb_Trap_{i}.png", 40, 48, 16f, false, i);
        }
        return trapSprites;
    }

    public static Sprite[] GetBreakSprites()
    {
        if (breakSprites != null && breakSprites.Length == 2 && breakSprites[0] != null)
            return breakSprites;

        breakSprites = new Sprite[2];
        for (int i = 0; i < 2; i++)
        {
            breakSprites[i] = LoadOrGenerateSprite($"Assets/ART/SpiderWeb/SpiderWeb_Break_{i}.png", 44, 48, 16f, false, i);
        }
        return breakSprites;
    }

    private static Sprite LoadOrGenerateSprite(string path, int width, int height, float ppu, bool isBullet, int frame)
    {
#if UNITY_EDITOR
        Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded != null) return loaded;
#endif

        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (tex.LoadImage(fileData))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            }
        }

        // Fallback procedural pixel art
        return isBullet ? GenerateProceduralBulletSprite(frame) : GenerateProceduralTrapSprite(frame);
    }

    private static Sprite GenerateProceduralBulletSprite(int frame)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[size * size];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        int cx = 16, cy = 16;
        float angleOffset = frame * 90f * Mathf.Deg2Rad;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= 6f)
                {
                    cols[y * size + x] = Color.white;
                }
                else if (d <= 7.5f)
                {
                    cols[y * size + x] = new Color(0.85f, 0.9f, 0.95f, 0.9f);
                }
            }
        }

        // Tia to vuon ra
        for (int i = 0; i < 4; i++)
        {
            float ang = angleOffset + (i * 90f) * Mathf.Deg2Rad;
            for (int r = 6; r <= 12; r++)
            {
                int px = Mathf.Clamp(cx + Mathf.RoundToInt(Mathf.Cos(ang) * r), 0, size - 1);
                int py = Mathf.Clamp(cy + Mathf.RoundToInt(Mathf.Sin(ang) * r), 0, size - 1);
                cols[py * size + px] = (r < 10) ? Color.white : new Color(0.85f, 0.9f, 1f, 0.8f);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private static Sprite GenerateProceduralTrapSprite(int frame)
    {
        int w = 40, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] cols = new Color[w * h];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        int cx = 20, cy = 24;

        // Nan to 8 huong
        for (int a = 0; a < 8; a++)
        {
            float ang = (a * 45f) * Mathf.Deg2Rad;
            for (int r = 4; r <= 20; r++)
            {
                int px = Mathf.Clamp(cx + Mathf.RoundToInt(Mathf.Cos(ang) * r), 0, w - 1);
                int py = Mathf.Clamp(cy + Mathf.RoundToInt(Mathf.Sin(ang) * r * 1.15f), 0, h - 1);
                cols[py * w + px] = new Color(0.95f, 0.95f, 1f, 0.85f);
            }
        }

        // Ken to o giua
        for (int y = 10; y <= 38; y++)
        {
            for (int x = 12; x <= 28; x++)
            {
                float dx = (x - cx) / 7.0f;
                float dy = (y - cy) / 13.0f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    bool stripe = ((x + y + frame * 2) % 3 == 0);
                    cols[y * w + x] = stripe ? Color.white : new Color(0.85f, 0.9f, 0.98f, 0.95f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    public static void SpawnWebBreakVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("WebBreak_VFX");
        fx.transform.position = pos;
        var comp = fx.AddComponent<WebBreakVisualEffect>();
        comp.Play();
    }
}

public class WebBreakVisualEffect : MonoBehaviour
{
    public void Play()
    {
        StartCoroutine(Routine());
    }

    private System.Collections.IEnumerator Routine()
    {
        Sprite[] breaks = SpiderWebSpriteFactory.GetBreakSprites();
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3200;
        sr.color = Color.white;

        for (int i = 0; i < breaks.Length; i++)
        {
            if (breaks[i] != null) sr.sprite = breaks[i];
            yield return new WaitForSeconds(0.12f);
        }

        // Fade out
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / 0.2f));
            transform.localScale += Vector3.one * (Time.deltaTime * 0.5f);
            yield return null;
        }

        Destroy(gameObject);
    }
}