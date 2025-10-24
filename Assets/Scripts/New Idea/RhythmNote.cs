using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RhythmNote
{
    public float time;
}

[Serializable]
public class RhythmLevel
{
    public string levelName;
    public float bpm;
    public string songFileName;
    public List<RhythmNote> notes = new List<RhythmNote>();
}
