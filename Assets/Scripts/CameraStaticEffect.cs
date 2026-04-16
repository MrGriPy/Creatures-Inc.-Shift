using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraStaticEffect : MonoBehaviour
{
    public static CameraStaticEffect instance;

    [Header("--- Réglages ---")]
    public RawImage staticImage;
    [Range(0f, 1f)] public float transparenceAmbiante = 0.15f;
    public float dureeFlash = 0.5f;

    [Range(0f, 1f)] public float intensiteFlash = 1.0f;

    private Texture2D noiseTexture;
    private Color[] pixels;
    // I removed the "private bool isFlashing" line which was causing issues

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        noiseTexture = new Texture2D(64, 64);
        noiseTexture.filterMode = FilterMode.Point;
        staticImage.texture = noiseTexture;
        pixels = new Color[noiseTexture.width * noiseTexture.height];

        SetAlpha(transparenceAmbiante);
    }

    void Update()
    {
        if (!staticImage.gameObject.activeInHierarchy) return;
        GenerateNoise();
    }

    void GenerateNoise()
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            float val = Random.value > 0.5f ? 1f : 0f;
            pixels[i] = new Color(val, val, val, staticImage.color.a);
        }

        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
    }

    public void TriggerStatic()
    {
        // StopAllCoroutines is very important here:
        // If we spam the button, it cuts the old flash to immediately start a new one
        StopAllCoroutines();
        StartCoroutine(PlayStaticFlash());
    }

    IEnumerator PlayStaticFlash()
    {
        // 1. Heavy Static (FIX: We use your variable here!)
        SetAlpha(intensiteFlash);

        // 2. We wait a little bit 
        yield return new WaitForSeconds(0.1f);

        // 3. Progressive return to normal
        float timer = 0f;
        while (timer < dureeFlash)
        {
            timer += Time.deltaTime;

            // FIX: The Lerp must start from 'intensiteFlash', not from 1f
            float newAlpha = Mathf.Lerp(intensiteFlash, transparenceAmbiante, timer / dureeFlash);

            SetAlpha(newAlpha);
            yield return null;
        }

        // Safety: return to normal
        SetAlpha(transparenceAmbiante);
    }

    void SetAlpha(float alpha)
    {
        Color c = staticImage.color;
        c.a = alpha;
        staticImage.color = c;
    }
}