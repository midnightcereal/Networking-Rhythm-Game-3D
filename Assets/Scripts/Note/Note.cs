using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public Transform hitLine;

    [Header("Hold Note")]
    public bool isHold = false;
    public float holdDuration = 0f;

    [Header("Pulse")]
    public float pulseScaleMin = 1.5f;
    public float pulseScaleMax = 1.7f;
    public float pulseSpeed = 8f;

    private bool hit = false;
    private bool isHolding = false;
    private float holdTimer = 0f;

    private Transform holdVisual; // hold line
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (isHold)
        {
            // Create hold visual line going upward from the top of the note
            holdVisual = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            holdVisual.parent = transform;

            float lineLength = holdDuration * speed;
            holdVisual.localScale = new Vector3(0.8f, lineLength, 0.8f);
            holdVisual.localPosition = new Vector3(0, transform.localScale.y / 2f + lineLength / 2f, 0);

            Destroy(holdVisual.GetComponent<Collider>());
        }
    }

    private void Update()
    {
        // Move note downward
        if (!hit)
        {
            transform.position += Vector3.down * speed * Time.deltaTime;

            if (!isHold && transform.position.y < hitLine.position.y - 0.1f)
                Miss();
        }

        // Hold note logic
        if (hit && isHold)
        {
            if (isHolding)
            {
                holdTimer += Time.deltaTime;
                float remaining = Mathf.Clamp(holdDuration - holdTimer, 0f, holdDuration);

                // Update hold visual length
                if (holdVisual != null)
                {
                    holdVisual.localScale = new Vector3(0.8f, remaining * speed, 0.8f);
                    holdVisual.localPosition = new Vector3(0, transform.localScale.y / 2f + holdVisual.localScale.y / 2f, 0);
                }

                // Pulse the note scale
                float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f); // 0 -> 1
                float scale = Mathf.Lerp(pulseScaleMin, pulseScaleMax, pulse);
                transform.localScale = originalScale * scale;

                if (holdTimer >= holdDuration)
                {
                    DestroyNote();
                }
            }
            else
            {
                // Released early -> miss
                Miss();
            }
        }
    }

    public void Hit(bool holdingKey = false)
    {
        if (hit) return;
        hit = true;

        // Snap to hit line
        Vector3 pos = transform.position;
        pos.y = hitLine.position.y;
        transform.position = pos;

        // Instant pop
        transform.localScale = originalScale * pulseScaleMin;

        if (isHold)
        {
            isHolding = holdingKey;
        }
        else
        {
            DestroyNote();
        }
    }

    public void ReleaseHold()
    {
        if (isHold && hit)
        {
            isHolding = false;
        }
    }

    private void Miss()
    {
        Debug.Log("Miss!");
        DestroyNote();
    }

    private void DestroyNote()
    {
        Destroy(gameObject);
    }
}