using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissEffect : MonoBehaviour
{
    public static MissEffect Instance { get; private set; }

    [Header("Flash Effect Settings")]
    public Image imageOverlay;
    public TextMeshProUGUI gameOverText;
    public Color flashColor = new Color(0f, 0f, 0f, 0.6f);
    public float flashDuration = 0.3f;

    private Coroutine flashRoutine;
    private bool isGameOver = false;
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (imageOverlay != null)
            imageOverlay.color = new Color(0, 0, 0, 0);

        gameOverText.enabled = false;
    }

    public void TriggerMissFlash()
    {
        if (isGameOver)
        {
            return;
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void TriggerPermanentFlash()
    {
        if (isGameOver) return;

        isGameOver = true;

        if (imageOverlay != null)
            imageOverlay.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    gameOverText.enabled = true;
        Debug.Log("[MissEffect] Permanent GAME OVER flash activated!");
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

    ///<summary>Called when results screen appears so the game over canvas disappears</summary>
    public void HideGameOver()
    {
        isGameOver = false;

        if (imageOverlay != null)
            imageOverlay.color = Color.clear;

        if (gameOverText != null)
            gameOverText.enabled = false;

        Debug.Log("[MissEffect] Game Over overlay hidden for results screen");
    }

    public void ResetGameOverState()
    {
        isGameOver = false;
        if (imageOverlay != null) imageOverlay.color = Color.clear;
        gameOverText.enabled = false;
    }
}