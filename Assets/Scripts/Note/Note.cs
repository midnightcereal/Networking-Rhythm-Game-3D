using UnityEngine;
using System.Collections;

public class Note : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public Transform hitLine;

    [Header("Hold Note Settings")]
    public bool isHold = false;
    public float holdDuration = 0f;
    public Color holdColor = Color.white;
    public Material holdMaterial;

    private bool isHit = false;
    private bool isHolding = false;
    private bool holdCompleted = false;
    private float holdTimer = 0f;
    private Transform holdLine;
    private Vector3 originalScale;
    private const float holdGrace = 0.15f; //early/late release grace period

    private void Start()
    {
        originalScale = transform.localScale;

        if (isHold)
        {
            //Create the hold line visually extending upward from the note
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(line.GetComponent<BoxCollider>());
            holdLine = line.transform;
            holdLine.SetParent(transform);
            holdLine.localPosition = Vector3.up * (holdDuration * speed / 2f);
            holdLine.localScale = new Vector3(0.15f, holdDuration * speed, 0.1f);

            Renderer rend = holdLine.GetComponent<Renderer>();

            if (holdMaterial != null)
            {
                //Set hold line material
                rend.material = holdMaterial;
                rend.material.color = holdColor;
            }
            else
            {
                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = holdColor;
                rend.material = mat;
            }
        }
    }

    private void Update()
    {
        if (!isHit)
        {
            transform.position += Vector3.down * speed * Time.deltaTime;

            if (transform.position.y < hitLine.position.y - 1f)
                Miss();
        }
        else if (isHold && isHolding)
        {
            holdTimer += Time.deltaTime;
            float remaining = Mathf.Clamp(holdDuration - holdTimer, 0f, holdDuration);

            //Pulse effect only on the note
            float pulse = Mathf.Sin(Time.time * 50f) * 0.1f + 1.1f;
            transform.localScale = originalScale * pulse;

            if (holdLine)
            {
                //Keep line visually stable (no pulsing movement)
                float baseWidth = 0.2f;
                float baseDepth = 0.1f;

                holdLine.localScale = new Vector3(baseWidth / pulse, remaining * speed, baseDepth / pulse);

                //Keep vertical position stable regardless of pulse
                holdLine.localPosition = Vector3.up * (remaining * speed / 2f);
            }

            //Auto complete when finished holding
            if (holdTimer >= holdDuration)
                HoldComplete();
        }
    }


    public void Hit()
    {
        if (isHit) return;
        isHit = true;

        transform.position = new Vector3(transform.position.x, hitLine.position.y, transform.position.z);

        if (isHold)
        {
            isHolding = true;
            ComboManager.Instance.StartHoldPulse();
            ComboManager.Instance.AddCombo(false);
            transform.localScale = originalScale * 1.3f;
        }
        else
        {
            ComboManager.Instance.AddCombo(false);
            StartCoroutine(PopThenDestroy());
        }
    }

    public void ReleaseHold()
    {
        if (!isHold || !isHolding) return;

        if (holdTimer >= holdDuration - holdGrace)
        {
            HoldComplete();
        }
        else
        {
            ComboManager.Instance.StopHoldPulse();
            Debug.Log("Hold released too early! | Missed");
            Miss();
        }
    }

    private void HoldComplete()
    {
        if (!isHolding || holdCompleted) return;

        holdCompleted = true;
        isHolding = false;

        //Add second combo point without restarting hold pulse
        ComboManager.Instance.AddCombo(false);
        //Stop the pulse
        ComboManager.Instance.StopHoldPulse();
        StartCoroutine(ShrinkAndDestroy());
    }

    private IEnumerator PopThenDestroy()
    {
        float duration = 0.05f;
        float timer = 0f;
        Vector3 start = transform.localScale;
        Vector3 end = start * 1.6f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, end, timer / duration);
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);
        Destroy(gameObject);
    }

    private IEnumerator ShrinkAndDestroy()
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
        ComboManager.Instance.ResetCombo();
        Destroy(gameObject);
    }
}