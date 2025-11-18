using System.Collections;
using UnityEngine;
using TMPro;

public class SongIntroUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup titleGroup;           //CanvasGroup with song name + artist
    public TMP_Text songTitleText;           //Song name
    public TMP_Text songArtistText;          //Artist name
    public TMP_Text difficultyText;          //Difficulty

    public CanvasGroup gameplayUIGroup;      //Main gameplay HUD
    public TMP_Text countdownText;           //Countdown numbers

    [Header("Intro Settings")]
    public float titleFadeDuration = 0.5f;   //Fade in/out duration
    public float titleDisplayTime = 2.0f;    //Time the title stays fully visible
    public float uiSlideDuration = 0.5f;     //Fade in time for gameplay HUD
    public float countdownDelay = 0.5f;      //Delay before countdown starts

    private void Awake()
    {
        if (titleGroup != null) titleGroup.alpha = 0;
        if (gameplayUIGroup != null) gameplayUIGroup.alpha = 0;
        if (countdownText != null) countdownText.text = "";
    }

    ///<summary>
    ///Plays the intro sequence before the song begins.
    ///</summary>
    public IEnumerator PlayIntroSequence(string songName, string artistName, int difficulty)
    {
        if (titleGroup == null || gameplayUIGroup == null || countdownText == null)
        {
            Debug.LogError("SongIntroUI: Missing one or more CanvasGroup/Text references!");
            yield break;
        }

        songTitleText.text = songName;
        songArtistText.text = artistName;
        difficultyText.text = "Difficulty: " + difficulty.ToString() + "/5";

        //SONG TITLE + ARTIST PHASE
        yield return FadeCanvas(titleGroup, 0f, 1f, titleFadeDuration);
        yield return new WaitForSeconds(titleDisplayTime);
        yield return FadeCanvas(titleGroup, 1f, 0f, titleFadeDuration);

        //SLIDE IN MAIN UI
        yield return FadeCanvas(gameplayUIGroup, 0f, 1f, uiSlideDuration);
        yield return new WaitForSeconds(countdownDelay);

        //COUNTDOWN PHASE
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.6f);
        countdownText.text = "";

        //Spawn The UI Prefab
        var uiSpawner = FindObjectOfType<GameplayUISpawner>();
        if (uiSpawner != null)
            uiSpawner.RequestUISpawnAfterIntro();
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        group.alpha = to;
    }
}