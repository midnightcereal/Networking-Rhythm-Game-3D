using System.Collections;
using UnityEngine;

public class EditorNote : MonoBehaviour
{
    [HideInInspector] public float originalY;

    public int lane = 0;
    public bool isHold = false;
    public float holdDuration = 0f;   // in seconds
    public float time = 0f;           // song time when note should appear

    [HideInInspector] public bool isDragging = false;
    [HideInInspector] public bool isHit = false;
    [HideInInspector] public bool isBeingHeld = false;

    [Header("Hold Line Variables")]
    public Material holdLineMaterial;
    private Transform holdLine;

    private Coroutine holdPulseRoutine;

    private void Awake()
    {
        originalY = transform.position.y;
    }

    private void Update()
    {
        if (EditorSongManager.Instance == null || EditorSongManager.Instance.audioSource == null || EditorSongManager.Instance.audioSource.clip == null)
            return;

        if (isDragging) return;

        // Move only if not hit and not being held by autoplayer
        if (!isHit && !EditorAutoPlayer.IsPlayingNote(this) && EditorSongManager.Instance.isPlayingFromCamera)
        {
            float songTime = EditorSongManager.Instance.audioSource.time;
            float y = EditorSongManager.Instance.trackStartY + (time - songTime) / EditorSongManager.Instance.speedMultiplier;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
    }

    public void UpdateHoldVisual(float remainingLength)
    {
        if (!isHold) return;

        if (holdLine == null)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(line.GetComponent<BoxCollider>());
            holdLine = line.transform;
            holdLine.SetParent(transform, false);
            line.transform.localPosition = Vector3.zero;

            if (holdLineMaterial != null)
            {
                Renderer rend = holdLine.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = holdLineMaterial;
            }
        }

        holdLine.localScale = new Vector3(0.15f, remainingLength / EditorSongManager.Instance.speedMultiplier, 0.1f);
        holdLine.localPosition = new Vector3(0f, remainingLength / (2f * EditorSongManager.Instance.speedMultiplier), -0.1f);
    }

    public void PlayTapPop(float amplitude = 0.15f, float duration = 0.2f)
    {
        StartCoroutine(TapPopCoroutine(amplitude, duration));
    }

    private IEnumerator TapPopCoroutine(float amplitude, float duration)
    {
        Vector3 start = transform.localScale;
        Vector3 target = start * (1f + amplitude);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, timer / duration);
            yield return null;
        }

        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(target, start, timer / duration);
            yield return null;
        }

        transform.localScale = start;
        gameObject.SetActive(false);
    }

    public void StartHoldPulse()
    {
        if (!isBeingHeld)
            holdPulseRoutine = StartCoroutine(HoldPulseCoroutine());
    }

    private IEnumerator HoldPulseCoroutine()
    {
        if (!holdLine) yield break;

        isBeingHeld = true;
        float elapsed = 0f;

        while (isBeingHeld && elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Clamp(holdDuration - elapsed, 0f, holdDuration);

            holdLine.localScale = new Vector3(0.15f, remaining / EditorSongManager.Instance.speedMultiplier, 0.1f);
            holdLine.localPosition = new Vector3(0f, remaining / (2f * EditorSongManager.Instance.speedMultiplier), 0f);

            yield return null;
        }

        isBeingHeld = false;
        gameObject.SetActive(false);
    }

    public void StopHoldPulse()
    {
        isBeingHeld = false;
        if (holdPulseRoutine != null)
        {
            StopCoroutine(holdPulseRoutine);
            holdPulseRoutine = null;
        }
    }

    public void ResetNote()
    {
        isHit = false;
        StopHoldPulse();
        isBeingHeld = false;

        float y = EditorSongManager.Instance.trackStartY + (time - 0f) / EditorSongManager.Instance.speedMultiplier;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        if (isHold)
            UpdateHoldVisual(holdDuration);

        gameObject.SetActive(true);
    }
}