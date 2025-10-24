using System.IO;
using UnityEngine;

public class RhythmLevelLoader : MonoBehaviour
{
    [Header("Level Settings")]
    public string levelFileName;

    [Header("References")]
    public NoteSpawner spawner;
    public AudioSource audioSource;

    void Start()
    {
        if (spawner == null || audioSource == null)
        {
            Debug.LogError("Spawner or AudioSource not assigned!");
            return;
        }

        RhythmLevel level = LoadLevel(levelFileName);
        if (level == null) return;

        //Load song from Resources/Songs/
        AudioClip clip = Resources.Load<AudioClip>("Songs/" + level.songFileName);
        if (clip == null)
        {
            Debug.LogError("Audio clip not found in Resources/Songs: " + level.songFileName);
            return;
        }

        audioSource.clip = clip;

        //Assign level to spawner
        spawner.level = level;
        spawner.audioSource = audioSource;

        audioSource.Play();
    }

    RhythmLevel LoadLevel(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Levels", fileName + ".json");

        if (!File.Exists(path))
        {
            Debug.LogError("Level JSON not found at: " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        RhythmLevel level = JsonUtility.FromJson<RhythmLevel>(json);

        if (level == null)
        {
            Debug.LogError("Failed to parse JSON: " + path);
            return null;
        }

        Debug.Log($"Loaded level '{level.levelName}' with {level.notes.Count} notes");
        return level;
    }
}