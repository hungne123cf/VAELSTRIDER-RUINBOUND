using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TorchFlicker : MonoBehaviour
{
    private Light2D light2D;

    [SerializeField] private float minIntensity = 0.85f;
    [SerializeField] private float maxIntensity = 1.35f;
    [SerializeField] private float flickerSpeed = 7.0f;
    [SerializeField] private bool isMysticPulse = false;

    private float baseIntensity;
    private float seed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        if (light2D != null)
        {
            baseIntensity = light2D.intensity;
        }
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (light2D == null) return;

        if (isMysticPulse)
        {
            // Breathing pulse effect for shrines and portals
            float wave = (Mathf.Sin((Time.time + seed) * 1.8f) + 1f) * 0.5f;
            light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);
        }
        else
        {
            // Organic candle/torch flicker using Perlin noise
            float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + seed, 0f);
            light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }
    }

    public void SetMysticPulse()
    {
        isMysticPulse = true;
        minIntensity = 1.1f;
        maxIntensity = 1.8f;
    }
}
