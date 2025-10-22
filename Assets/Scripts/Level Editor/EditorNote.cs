using UnityEngine;

public class EditorNote : MonoBehaviour
{
    public int lane = 0;
    public bool isHold = false;
    public float holdDuration = 0f;   // in seconds
    public float time = 0f;           // time in song when note should appear

    [HideInInspector]
    public bool isDragging = false;

    [Header("Hold Line Variables")]
    public Material holdLineMaterial;
    private Transform holdLine;
    private EditorSongManager manager;

    private void Start()
    {
        manager = FindObjectOfType<EditorSongManager>();
        if (!manager)
            Debug.LogWarning("EditorSongManager not found!");
    }

    private void Update()
    {
        if (manager == null || manager.audioSource == null || manager.audioSource.clip == null) return;

        float songTime = manager.audioSource.time; // seconds into the song
        float y = (time - songTime) / manager.speedMultiplier;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public void UpdateHoldVisual(float length)
    {
        if (!isHold) return;

        // Create hold line if it doesn't exist
        if (holdLine == null)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(line.GetComponent<BoxCollider>());
            holdLine = line.transform;
            holdLine.SetParent(transform, false);

            if (holdLineMaterial != null)
            {
                Renderer rend = holdLine.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = holdLineMaterial;
            }
        }

        // Visual: scale line and position so bottom is at note center, extends upward
        holdLine.localScale = new Vector3(0.15f, length, 0.1f);
        holdLine.localPosition = new Vector3(0f, length / 2f, -0.1f);

        // Collider: match the line visually, offset so bottom is at note center
        SetupCollider(length);
    }

    public void SetupCollider(float length)
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // Offset so bottom aligns with note pivot
        col.center = new Vector3(0f, length / 2f, 0f);
        col.size = new Vector3(0.15f, length, 0.1f);
        col.enabled = true;
    }
}