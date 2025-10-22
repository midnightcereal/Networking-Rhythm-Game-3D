using UnityEngine;

public class Note : MonoBehaviour
{
    public int lane = 0;
    public bool isHold = false;
    public float holdDuration = 0f; // in seconds
    public float time = 0f;         // when note should reach hitLine

    [Header("References")]
    public Transform hitLine;
    public Material holdLineMaterial;

    [HideInInspector] public bool isHit = false;
    private bool isHolding = false;
    private bool holdCompleted = false;
    private float holdTimer = 0f;
    private Transform holdLine;
    private Vector3 originalScale;

    [HideInInspector] public float speedMultiplier = 0.1f; // set from SongManager
    private AudioSource audioSource;

    private void Start()
    {
        originalScale = transform.localScale;
        audioSource = FindObjectOfType<AudioSource>();

        if (isHold && holdDuration > 0f)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(line.GetComponent<BoxCollider>());
            holdLine = line.transform;
            holdLine.SetParent(transform, false);

            holdLine.localScale = new Vector3(0.15f, holdDuration / speedMultiplier, 0.1f);
            holdLine.localPosition = new Vector3(0f, holdDuration / (2f * speedMultiplier), 0f);

            if (holdLineMaterial != null)
                holdLine.GetComponent<Renderer>().material = holdLineMaterial;
        }
    }

    private void Update()
    {
        if (!audioSource) return;

        if (!isHit)
        {
            // Exact editor-style position calculation
            float songTime = audioSource.time;
            float y = (time - songTime) / speedMultiplier + hitLine.position.y;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);

            if (y <= hitLine.position.y - 0.1f)
                Miss();
        }
        else if (isHold && isHolding)
        {
            holdTimer += Time.deltaTime;
            float remaining = Mathf.Clamp(holdDuration - holdTimer, 0f, holdDuration);

            if (holdLine)
            {
                holdLine.localScale = new Vector3(0.15f, remaining / speedMultiplier, 0.1f);
                holdLine.localPosition = new Vector3(0f, remaining / (2f * speedMultiplier), 0f);
            }

            if (holdTimer >= holdDuration)
                HoldComplete();
        }
    }

    public void Hit()
    {
        if (isHit) return;
        isHit = true;

        if (isHold)
        {
            isHolding = true;
            transform.localScale = originalScale * 1.3f;
        }
        else
        {
            StartCoroutine(ShrinkAndDestroy());
        }
    }

    public void ReleaseHold()
    {
        if (!isHold || !isHolding) return;

        if (holdTimer >= holdDuration)
            HoldComplete();
        else
            Miss();
    }

    private void HoldComplete()
    {
        if (!isHolding || holdCompleted) return;

        holdCompleted = true;
        isHolding = false;

        StartCoroutine(ShrinkAndDestroy());
    }

    private System.Collections.IEnumerator ShrinkAndDestroy()
    {
        float duration = 0.05f;
        float timer = 0f;
        Vector3 start = transform.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, timer / duration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Miss()
    {
        Debug.Log("Miss!");
        Destroy(gameObject);
    }
}