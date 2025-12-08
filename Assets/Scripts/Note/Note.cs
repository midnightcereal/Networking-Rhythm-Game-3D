using UnityEngine;
using System.Collections;

public class Note : MonoBehaviour
{
    public int lane = 0;
    public bool isHold = false;
    public float holdDuration = 0f; //in seconds
    public float time = 0f;         //when note should reach hitLine

    [Header("References")]
    public Transform hitLine;
    public Material holdLineMaterial;

    [HideInInspector] public bool isHit = false;
    private bool isHolding = false;
    private bool holdCompleted = false;
    private float holdTimer = 0f;
    private Transform holdLine;
    private Vector3 originalScale;

    [HideInInspector] public float speedMultiplier = 0.1f;
    private float songTimer = 0f;
    //private AudioSource audioSource;

    private void Start()
    {
        originalScale = transform.localScale;
        //audioSource = FindObjectOfType<AudioSource>();

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
        //if (!audioSource) return;

        songTimer += Time.deltaTime;

        if (!isHit)
        {
            //Move note down
            float y = (time - songTimer) / speedMultiplier + hitLine.position.y;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);

            if (y <= hitLine.position.y - 0.1f)
                Miss();
        }
        else if (isHold && isHolding)
        {
            //Rapid pulsing while held
            float pulse = 0.03f * Mathf.Sin(Time.time * 50f);
            transform.localScale = originalScale * 1.3f * (1f + pulse);

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

        float dist = Mathf.Abs(transform.position.y - hitLine.position.y);
        bool isPerfect = dist <= 0.1f;

        ResultsManager.Instance.RegisterHit(isPerfect);
        Debug.Log("[ResultsManager] Registered Hit, PERFECT?: " + isPerfect);

        if (isHold)
        {
            isHolding = true;

            //Combo for hold start
            ComboManager.Instance.AddCombo(true, this);

            //Start combo text pulse
            ComboManager.Instance.StartHoldPulse(this);
        }
        else
        {
            //Combo for tap
            ComboManager.Instance.AddCombo(false, this);

            //Pop note animation
            StartCoroutine(PopAndDestroy());
        }
    }

    public void ReleaseHold()
    {
        if (!isHold || !isHolding) return;

        //Released too early (before the hold is done)
        if (holdTimer < holdDuration)
        {
            //Reset combo on early release
            ComboManager.Instance.ResetCombo();
            Miss();
        }
        else
        {
            HoldComplete();
        }

        //Stop combo text pulse immediately
        ComboManager.Instance.StopHoldPulse(this);
        transform.localScale = originalScale;
    }

    private void HoldComplete()
    {
        if (!isHolding || holdCompleted) return;

        holdCompleted = true;
        isHolding = false;

        //Extra combo for completing hold
        ComboManager.Instance.AddCombo(true, this);

        //Pop note on hold completion
        StartCoroutine(PopAndDestroy());

        //Stop combo text pulse
        ComboManager.Instance.StopHoldPulse(this);
    }

    private IEnumerator PopAndDestroy()
    {
        float duration = 0.035f;
        float timer = 0f;
        Vector3 start = transform.localScale;
        Vector3 target = start * 2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, timer / duration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Miss()
    {
        ResultsManager.Instance.RegisterMiss();
        Debug.Log("[ResultsManager] Registered Miss");

        transform.localScale = originalScale;
        Destroy(gameObject);
        ComboManager.Instance.ResetCombo();
    }
}