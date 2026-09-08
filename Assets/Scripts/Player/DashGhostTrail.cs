using UnityEngine;
using System.Collections;

public class DashGhostTrail : MonoBehaviour
{
    [Header("Ghost Settings")]
    [SerializeField] private float spawnInterval = 0.045f;
    [SerializeField] private float ghostFadeTime = 0.35f;
    [SerializeField] private Color ghostColor = new Color(0.3f, 0.75f, 1f, 0.65f); // Cyan spectral shadow

    private SpriteRenderer sr;
    private PlayerController playerController;
    private float lastSpawnTime = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (playerController != null && playerController.IsDashing())
        {
            if (Time.time - lastSpawnTime >= spawnInterval)
            {
                lastSpawnTime = Time.time;
                SpawnGhost();
            }
        }
    }

    private void SpawnGhost()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = sr.sprite;
        ghostSr.flipX = sr.flipX;
        ghostSr.sortingLayerID = sr.sortingLayerID;
        ghostSr.sortingOrder = sr.sortingOrder - 1;
        ghostSr.color = ghostColor;

        StartCoroutine(FadeGhostRoutine(ghost, ghostSr));
    }

    private IEnumerator FadeGhostRoutine(GameObject ghostObj, SpriteRenderer ghostSr)
    {
        float elapsed = 0f;
        Color startColor = ghostSr.color;

        while (elapsed < ghostFadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / ghostFadeTime);
            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(ghostObj);
    }
}
