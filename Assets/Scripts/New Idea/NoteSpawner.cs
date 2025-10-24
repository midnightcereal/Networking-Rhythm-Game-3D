using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public RhythmLevel level;
    public AudioSource audioSource;
    public GameObject notePrefab;
    public Transform spawnCenter;

    [Header("Orbit Settings")]
    public float maxOrbitRadius = 5f;
    public int orbitSlots = 10;
    public float bpm = 120f;
    public float flyInDuration = 0.6f;

    [Header("Pulse Settings")]
    public float pulseIntensity = 1f;
    public float pulseSensitivity = 5f;

    private Queue<RhythmNote> noteQueue = new Queue<RhythmNote>();
    private OrbitingNote[] orbitNotes;
    private bool[] slotOccupied;

    private OrbitPulse orbitPulse;

    void Start()
    {
        orbitNotes = new OrbitingNote[orbitSlots];
        slotOccupied = new bool[orbitSlots];

        //Spawn x orbiting placeholder notes evenly spaced around circle
        for (int i = 0; i < orbitSlots; i++)
        {
            GameObject noteObj = Instantiate(notePrefab, spawnCenter.position, Quaternion.identity);
            OrbitingNote orbit = noteObj.GetComponent<OrbitingNote>();

            orbit.centerPoint = spawnCenter;
            orbit.audioSource = audioSource;
            orbit.maxOrbitRadius = maxOrbitRadius;
            orbit.initialAngleOffset = i * Mathf.PI * 2f / orbitSlots;
            orbit.isPlaceholder = true;

            orbitNotes[i] = orbit;
            slotOccupied[i] = false;
        }

        //Queue level notes
        if (level != null)
        {
            foreach (var n in level.notes)
                noteQueue.Enqueue(n);
        }

        //Setup OrbitPulse
        orbitPulse = spawnCenter.GetComponent<OrbitPulse>();
        if (orbitPulse == null)
            orbitPulse = spawnCenter.gameObject.AddComponent<OrbitPulse>();

        orbitPulse.audioSource = audioSource;
        orbitPulse.baseRadius = maxOrbitRadius;
        orbitPulse.pulseIntensity = pulseIntensity;
        orbitPulse.sensitivity = pulseSensitivity;
        orbitPulse.orbitNotes = orbitNotes;
    }

    void Update()
    {
        if (audioSource == null || level == null) return;

        float songTime = audioSource.time;

        //Free slots from finished notes
        for (int i = 0; i < orbitNotes.Length; i++)
        {
            if (orbitNotes[i].isPlaceholder)
                slotOccupied[i] = false;
        }

        //Spawn next note if available and slot free
        if (noteQueue.Count > 0)
        {
            RhythmNote nextNote = noteQueue.Peek();
            float spawnTime = nextNote.time - flyInDuration;

            if (songTime >= spawnTime)
            {
                int freeSlot = GetFreeSlotIndex();
                if (freeSlot != -1)
                {
                    orbitNotes[freeSlot].Activate(nextNote.time, flyInDuration);
                    slotOccupied[freeSlot] = true;
                    noteQueue.Dequeue();
                }
            }
        }
    }

    private int GetFreeSlotIndex()
    {
        for (int i = 0; i < orbitSlots; i++)
        {
            if (!slotOccupied[i])
                return i;
        }
        return -1;
    }
}