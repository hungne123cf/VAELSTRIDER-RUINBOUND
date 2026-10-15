using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hiệu ứng Băng Địa Chấn (Ice Ground Slam VFX) của Minotaur Titan Berserker
/// Thay thế cho vết nứt đất procedual, sử dụng bộ sprite Ice VFX từ Assets/ART/Player4/UI/Separated Frames
/// Bao gồm 3 giai đoạn: Start (1..9) -> Active (1..9) -> Ending (1..18)
/// </summary>
public class IceSlamVFX : MonoBehaviour
{
    private static Sprite[] startFrames = null;
    private static Sprite[] activeFrames = null;
    private static Sprite[] endingFrames = null;
    private static bool isInitialized = false;

    public static void EnsureFramesLoaded()
    {
        if (isInitialized && startFrames != null && startFrames.Length > 0) return;

        startFrames = LoadFrames("Start", 9);
        activeFrames = LoadFrames("Active", 9);
        endingFrames = LoadFrames("Ending", 18);
        isInitialized = true;
    }

    private static Sprite[] LoadFrames(string prefix, int count)
    {
        List<Sprite> list = new List<Sprite>();

        for (int i = 1; i <= count; i++)
        {
            string fileName = $"Ice VFX 2 {prefix}{i}";
            Sprite s = null;

            // 1. Thử load từ Resources
            s = Resources.Load<Sprite>($"VFX/Player4_IceSlam/{fileName}");

#if UNITY_EDITOR
            // 2. Thử load từ AssetDatabase trong Editor
            if (s == null)
            {
                string path = $"Assets/ART/Player4/UI/Separated Frames/{fileName}.png";
                s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s == null)
                {
                    path = $"Assets/Resources/VFX/Player4_IceSlam/{fileName}.png";
                    s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
#endif

            // 3. Fallback: Đọc trực tiếp byte ảnh từ ổ đĩa nếu chưa kịp import
            if (s == null)
            {
                s = LoadRawPngSprite($"Assets/ART/Player4/UI/Separated Frames/{fileName}.png");
                if (s == null)
                {
                    s = LoadRawPngSprite($"Assets/Resources/VFX/Player4_IceSlam/{fileName}.png");
                }
            }

            if (s != null)
            {
                list.Add(s);
            }
        }

        return list.ToArray();
    }

    private static Sprite LoadRawPngSprite(string relativePath)
    {
        try
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, "..", relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Kích hoạt chuỗi trụ băng chấn địa lan tỏa theo hướng đập búa
    /// </summary>
    public static void Spawn(Vector3 origin, Vector2 dir, float length, float ratio)
    {
        EnsureFramesLoaded();

        GameObject root = new GameObject("IceSlam_FissureZone");
        root.transform.position = origin;
        var comp = root.AddComponent<IceSlamVFX>();
        comp.StartCoroutine(comp.FissureSequenceRoutine(origin, dir, length, ratio));
    }

    private IEnumerator FissureSequenceRoutine(Vector3 origin, Vector2 dir, float length, float ratio)
    {
        if (dir.sqrMagnitude < 0.001f) dir = Vector2.down;
        dir = dir.normalized;

        // Ánh sáng bộc phát băng giá tổng thể khu vực
        var mainLight = gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        mainLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        mainLight.color = new Color(0.35f, 0.85f, 1f, 1f);
        mainLight.intensity = 2.8f;
        mainLight.pointLightInnerRadius = 0.6f;
        mainLight.pointLightOuterRadius = length * 1.1f;

        // Sắp xếp các cọc băng thành 1 ĐƯỜNG THẲNG thẳng tắp trùng khớp với đường ngắm/tung chiêu
        float startDist = 0.85f;
        float endDist = startDist + length;
        int pillarCount = Mathf.Clamp(Mathf.RoundToInt(length / 0.55f), 6, 9);
        float stepDist = (endDist - startDist) / (pillarCount - 1);

        for (int i = 0; i < pillarCount; i++)
        {
            float d = startDist + (i * stepDist);
            Vector3 pos = origin + (Vector3)(dir * d);
            float scale = Mathf.Lerp(1.8f, 2.4f, ratio);
            float delay = i * 0.032f; // Sóng băng bộc phát nối tiếp thành đường thẳng
            SpawnSpikePillar(pos, scale, delay);
        }

        // Giảm dần ánh sáng theo thời gian
        float elapsed = 0f;
        float totalDuration = 1.6f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float life = elapsed / totalDuration;
            if (mainLight != null)
            {
                mainLight.intensity = Mathf.Lerp(2.8f, 0f, life);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnSpikePillar(Vector3 pos, float scale, float delay)
    {
        StartCoroutine(SpikePillarRoutine(pos, scale, delay));
    }

    private IEnumerator SpikePillarRoutine(Vector3 pos, float scale, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        GameObject spike = new GameObject("IceSpike_Cluster");
        spike.transform.SetParent(transform, true);
        spike.transform.position = pos;
        spike.transform.localScale = Vector3.one * scale;

        var sr = spike.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        // Sorting theo chuẩn depth sort hệ thống (base 5000) để luôn nổi trên sàn gạch và depth-sort tự nhiên với quái
        sr.sortingOrder = 5000 + Mathf.RoundToInt(-pos.y * 100) + 15;

        var spikeLight = spike.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        spikeLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        spikeLight.color = new Color(0.45f, 0.9f, 1f, 1f);
        spikeLight.intensity = 1.5f;
        spikeLight.pointLightInnerRadius = 0.3f;
        spikeLight.pointLightOuterRadius = 1.8f;

        float frameRate = 22f;
        float frameTime = 1f / frameRate;

        // 1. Giai đoạn Start (Cọc băng trồi lên từ mặt đất)
        if (startFrames != null && startFrames.Length > 0)
        {
            for (int i = 0; i < startFrames.Length; i++)
            {
                if (sr == null) yield break;
                sr.sprite = startFrames[i];
                yield return new WaitForSeconds(frameTime);
            }
        }

        // 2. Giai đoạn Active (Băng tích tụ tỏa hàn khí lạnh)
        if (activeFrames != null && activeFrames.Length > 0)
        {
            for (int i = 0; i < activeFrames.Length; i++)
            {
                if (sr == null) yield break;
                sr.sprite = activeFrames[i];
                yield return new WaitForSeconds(frameTime);
            }
        }

        // 3. Giai đoạn Ending (Băng tan vỡ vụn thành tinh thể tuyết)
        if (endingFrames != null && endingFrames.Length > 0)
        {
            for (int i = 0; i < endingFrames.Length; i++)
            {
                if (sr == null) yield break;
                sr.sprite = endingFrames[i];

                // Mờ dần ở các frame cuối
                float endRatio = (float)i / endingFrames.Length;
                if (endRatio > 0.5f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (endRatio - 0.5f) / 0.5f);
                    sr.color = new Color(1f, 1f, 1f, alpha);
                    if (spikeLight != null) spikeLight.intensity = 1.5f * alpha;
                }

                yield return new WaitForSeconds(frameTime * 0.9f);
            }
        }

        Destroy(spike);
    }
}
