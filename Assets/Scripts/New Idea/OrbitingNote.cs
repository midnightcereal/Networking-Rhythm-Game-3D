using UnityEngine;

public class OrbitingNote : MonoBehaviour
{
    public Transform centerPoint;
    public AudioSource audioSource;
    public float maxOrbitRadius = 5f;
    public float initialAngleOffset = 0f;
    public float globalRotationOffset = 0f;
    public float gracePeriod = 0.25f; // seconds note stays at hit area

    [HideInInspector] public bool isPlaceholder = true;

    private float hitTime;
    private float spawnTime;
    private float flyInDuration;
    private bool isFlyingIn = false;
    private bool hasHit = false;

    void Update()
    {
        if (centerPoint == null || audioSource == null) return;

        float songTime = audioSource.time;
        float angle = initialAngleOffset + globalRotationOffset;
        float radius = maxOrbitRadius;

        //Fly in movement
        if (isFlyingIn)
        {
            float t = Mathf.Clamp01((songTime - spawnTime) / flyInDuration);

            //Faster at start -> less slowdown near end
            t = Mathf.Pow(t, 0.8f);

            //Move from orbit -> center
            radius = Mathf.Lerp(maxOrbitRadius, 0f, t);

            if (t >= 1f)
            {
                isFlyingIn = false;
                hasHit = true;
            }
        }
        //Stay in center during grace period
        else if (hasHit)
        {
            radius = 0f;

            //After grace period free the slot
            if (songTime > hitTime + gracePeriod)
            {
                hasHit = false;
                isPlaceholder = true;
            }
        }

        //Set final position
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        transform.position = centerPoint.position + offset;
    }

    public void Activate(float targetHitTime, float flyDuration)
    {
        hitTime = targetHitTime;
        flyInDuration = flyDuration;
        //Ensures it starts moving early enough
        spawnTime = hitTime - flyDuration;
        isFlyingIn = true;
        isPlaceholder = false;
        hasHit = false;
    }
}