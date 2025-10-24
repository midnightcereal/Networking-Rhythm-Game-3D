//using Newtonsoft.Json;
//using System.Collections.Generic;
//using System.IO;
//using System.Xml;
//using UnityEngine;

//public class RhythmLevelEditor : MonoBehaviour
//{
//    public AudioSource audioSource;
//    public RhythmLevel currentLevel;

//    private bool isRecording = false;

//    void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.E)) // toggle editor mode
//        {
//            isRecording = !isRecording;
//            audioSource.Stop();
//            audioSource.time = 0;
//        }

//        if (!isRecording) return;

//        if (Input.GetKeyDown(KeyCode.Space)) // tap to register note
//        {
//            currentLevel.notes.Add(new RhythmNote { time = audioSource.time });
//            Debug.Log("Note added at " + audioSource.time);
//        }

//        if (Input.GetKeyDown(KeyCode.S)) // save level
//        {
//            string json = JsonConvert.SerializeObject(currentLevel, Formatting.Indented);
//            File.WriteAllText(Application.dataPath + "/" + currentLevel.levelName + ".json", json);
//            Debug.Log("Level saved!");
//        }
//    }
//}