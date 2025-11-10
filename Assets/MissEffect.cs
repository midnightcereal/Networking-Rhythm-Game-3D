using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MissEffect : MonoBehaviour
{
    [Header("Flash Effect Settings")]
    public Image imageOverlay;
    public Color flashColor = new Color(1f, 0f, 0f, 0.6f);
    public float flashDuration = 0.3f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (imageOverlay != null)
            imageOverlay.color = new Color(0, 0, 0, 0);
    }

    public void TriggerMissFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (imageOverlay == null)
            yield break;

        float timer = 0f;

        //Flash in
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashDuration;
            imageOverlay.color = Color.Lerp(Color.clear, flashColor, t);
            yield return null;
        }

        //Flash out
        timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float t = timer / flashDuration;
            imageOverlay.color = Color.Lerp(flashColor, Color.clear, t);
            yield return null;
        }

        imageOverlay.color = Color.clear;
    }
}