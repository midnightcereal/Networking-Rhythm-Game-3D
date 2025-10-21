using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CameraColourManager : MonoBehaviour
{
    private Camera cam;
    private List<Color> availableColours = new List<Color>();
    private List<Color> recentColours = new List<Color>();

    private int colourChangeBeats;
    private float bpm;
    private float beatInterval;
    private float nextColourTime;
    private AudioSource songAudio;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraColorManager must be attached to a Camera.");
        }
    }

    public void Initialize(string[] hexColours, float bpm, int colourChangeBeats, AudioSource audioSource)
    {
        availableColours.Clear();
        recentColours.Clear();
        this.bpm = bpm;
        this.colourChangeBeats = colourChangeBeats;
        this.songAudio = audioSource;

        //Convert hex colours to Unity Colour
        foreach (string hex in hexColours)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                availableColours.Add(c);
        }

        if (availableColours.Count == 0)
        {
            Debug.LogWarning("No valid colours found in level JSON.");
            return;
        }

        beatInterval = 60f / bpm;
        nextColourTime = 0f;
    }

    private void Update()
    {
        if (songAudio == null || availableColours.Count == 0)
            return;

        //Time when switch to the next colour (based on audio)
        if (songAudio.time >= nextColourTime)
        {
            ChangeToNextColour();
            nextColourTime += beatInterval * colourChangeBeats;
        }
    }

    private void ChangeToNextColour()
    {
        if (availableColours.Count == 0) return;

        Color newColour;
        int attempts = 0;

        //Avoid repeating the last 3 colours
        do
        {
            newColour = availableColours[Random.Range(0, availableColours.Count)];
            attempts++;
        }
        while (recentColours.Contains(newColour) && attempts < 10);

        cam.backgroundColor = newColour;

        //Update recent colours
        recentColours.Add(newColour);
        if (recentColours.Count > 3)
            recentColours.RemoveAt(0);
    }
}