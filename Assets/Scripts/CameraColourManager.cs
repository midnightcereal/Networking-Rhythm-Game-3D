using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraColourManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera targetCamera;
    public float transitionDuration = 1f;

    [Header("Beat Settings")]
    public float bpm = 120f;
    public int beatsPerColorChange = 4;

    private float secondsPerBeat;
    private float nextColorTime;
    private AudioSource musicSource;

    private List<Color> availableColors = new();
    private List<Color> recentColors = new();
    private Color currentColor;

    private void Start()
    {
        if (!targetCamera)
            targetCamera = Camera.main;

        musicSource = FindObjectOfType<AudioSource>();
        secondsPerBeat = 60f / bpm;
        nextColorTime = beatsPerColorChange * secondsPerBeat;

        if (availableColors.Count == 0)
        {
            availableColors.Add(Color.red);
            availableColors.Add(Color.blue);
            availableColors.Add(Color.green);
        }

        //Start with a random colour
        currentColor = availableColors[Random.Range(0, availableColors.Count)];
        targetCamera.backgroundColor = currentColor;
    }

    private void Update()
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if (musicSource.time >= nextColorTime)
        {
            PickNextColor();
            nextColorTime += beatsPerColorChange * secondsPerBeat;
        }
    }

    public void Initialize(float bpmValue, int beatsPerChange, List<Color> colors)
    {
        bpm = bpmValue;
        beatsPerColorChange = beatsPerChange;
        availableColors = colors;

        secondsPerBeat = 60f / bpm;
        nextColorTime = beatsPerColorChange * secondsPerBeat;

        if (availableColors.Count > 0)
            currentColor = availableColors[Random.Range(0, availableColors.Count)];

        if (targetCamera)
            targetCamera.backgroundColor = currentColor;
    }

    private void PickNextColor()
    {
        if (availableColors.Count == 0) return;

        Color nextColor;
        int attempts = 0;
        do
        {
            nextColor = availableColors[Random.Range(0, availableColors.Count)];
            attempts++;
        }
        while (recentColors.Contains(nextColor) && attempts < 50);

        //Update recent colours memory
        recentColors.Add(nextColor);
        if (recentColors.Count > 3)
            recentColors.RemoveAt(0);

        StopAllCoroutines();
        StartCoroutine(LerpColor(currentColor, nextColor));
        currentColor = nextColor;
    }

    private IEnumerator LerpColor(Color from, Color to)
    {
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            targetCamera.backgroundColor = Color.Lerp(from, to, t / transitionDuration);
            yield return null;
        }
        targetCamera.backgroundColor = to;
    }
}