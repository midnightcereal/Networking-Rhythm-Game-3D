using System.Collections;
using UnityEngine;

public class EditorNote : MonoBehaviour
{
    [HideInInspector] public float originalY;

    public int lane = 0;
    public bool isHold = false;
    public float holdDuration = 0f;   // in seconds
    public float time = 0f;           // when note should appear

    [HideInInspector] public bool isDragging = false;

    [HideInInspector] public bool isHit = false;
    [HideInInspector] public bool isBeingHeld = false;

    [Header("Hold Line Variables")]
    public Material holdLineMaterial;
    private Transform holdLine;

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
        if (!isHit && !EditorAutoPlayer.IsPlayingNote(this))
        {
            float songTime = EditorSongManager.Instance.audioSource.time;
            float y = (time - songTime) / EditorSongManager.Instance.speedMultiplier;
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

            // Move pivot to bottom
            line.transform.localPosition = Vector3.zero;

            if (holdLineMaterial != null)
            {
                Renderer rend = holdLine.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = holdLineMaterial;
            }
        }

        // Scale the line in Y according to remainingLength
        holdLine.localScale = new Vector3(0.15f, remainingLength, 0.1f);

        // Move line so bottom stays at note's position
        holdLine.localPosition = new Vector3(0f, remainingLength / 2f, -0.1f);

        //SetupCollider(remainingLength);
    }


    public void SetupCollider(float length)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, length / 2f, 0f);
        col.size = new Vector3(0.15f, length, 0.1f);
        col.enabled = true;
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
    }

    public void StartHoldPulse(float amplitude = 0.1f, float cycleDuration = 0.4f)
    {
        if (!isBeingHeld) StartCoroutine(HoldPulseCoroutine(amplitude, cycleDuration));
    }

    private IEnumerator HoldPulseCoroutine(float amplitude, float cycleDuration)
    {
        isBeingHeld = true;
        Vector3 originalScale = transform.localScale;
        float halfCycle = cycleDuration / 2f;

        while (isBeingHeld)
        {
            float timer = 0f;
            while (timer < halfCycle && isBeingHeld)
            {
                timer += Time.deltaTime;
                transform.localScale = originalScale * (1f + amplitude * (timer / halfCycle));
                yield return null;
            }

            timer = 0f;
            while (timer < halfCycle && isBeingHeld)
            {
                timer += Time.deltaTime;
                transform.localScale = originalScale * (1f + amplitude * (1f - timer / halfCycle));
                yield return null;
            }
        }

        transform.localScale = originalScale;
    }

    public void StopHoldPulse() => isBeingHeld = false;

    public void ResetNote()
    {
        isHit = false;
        isBeingHeld = false;
        transform.position = new Vector3(transform.position.x, originalY, transform.position.z);

        // Reset hold line to full length
        if (isHold)
            UpdateHoldVisual(holdDuration / EditorSongManager.Instance.speedMultiplier);

        StopHoldPulse();
        gameObject.SetActive(true);
    }
}