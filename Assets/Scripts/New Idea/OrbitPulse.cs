using UnityEngine;

[RequireComponent(typeof(Transform))]
public class OrbitPulse : MonoBehaviour
{
    public AudioSource audioSource;
    public float baseRadius = 5f;
    public float pulseIntensity = 1f;
    public float sensitivity = 5f;
    public OrbitingNote[] orbitNotes;

    private float[] samples = new float[512];

    void Update()
    {
        if (audioSource == null || orbitNotes == null || orbitNotes.Length == 0) return;

        //Get the spectrum data from the audio
        audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        //Amplitude estimate (average of first few bins)
        float amplitude = 0f;
        for (int i = 0; i < 16; i++)
        {
            amplitude += samples[i];
        }
        amplitude /= 16f;

        //Scale amplitude
        amplitude *= sensitivity;

        //New radius for this frame
        float radius = baseRadius + amplitude * pulseIntensity;

        //Apply radius to all orbiting notes
        foreach (var note in orbitNotes)
        {
            if (note != null)
                note.maxOrbitRadius = radius;
        }
    }
}