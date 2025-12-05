using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class ResultsManager : NetworkBehaviour
{
    public static ResultsManager Instance;

    [Header("Results UI")]
    public GameObject resultsCanvas;
    public TextMeshProUGUI leftResultsText;
    public TextMeshProUGUI rightResultsText;

    private AudioSource songAudioSource;
    private SongManager songManager;

    //LOCAL stats per player (each player tracks their own)
    private int localHits = 0;
    private int localPerfects = 0;
    private int localMisses = 0;
    private int localMaxCombo = 0;
    private int currentCombo = 0;

    //Final synced stats
    private readonly int[] finalHits = new int[2];
    private readonly int[] finalPerfects = new int[2];
    private readonly int[] finalMisses = new int[2];
    private readonly int[] finalMaxCombo = new int[2];

    private int playersReported = 0;
    private bool songEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (resultsCanvas) resultsCanvas.SetActive(false);
    }

    private void Start()
    {
        songManager = FindObjectOfType<SongManager>();
        if (songManager != null)
        {
            songAudioSource = songManager.audioSource;
        }
        else
        {
            Debug.LogError("ResultsManager: SongManager not found!");
        }
    }

    private void Update()
    {
        if (songEnded || songAudioSource == null || songAudioSource.clip == null) return;

        if (songAudioSource.time >= songAudioSource.clip.length - 0.2f)
        {
            songEnded = true;
            //Song ended -> local player submits their stats
            SubmitLocalResults();
        }
    }

    //Called from Note.Hit() and Note.Miss()
    public void RegisterHit(bool isPerfect)
    {
        localHits++;
        if (isPerfect) localPerfects++;

        currentCombo++;
        if (currentCombo > localMaxCombo) localMaxCombo = currentCombo;
    }

    public void RegisterMiss()
    {
        localMisses++;
        currentCombo = 0;
    }

    public void OnComboBreak()
    {
        currentCombo = 0;
    }

    ///<summary>Called when song ends — each player submits their own stats</summary>
    public void SubmitLocalResults()
    {
        if (!IsClient) return;

        //Send to server
        SubmitStatsServerRpc(localHits, localPerfects, localMisses, localMaxCombo);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitStatsServerRpc(int hits, int perfects, int misses, int maxCombo, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int side = GameplayUI.Instance.GetPlayerSide(clientId);

        finalHits[side] = hits;
        finalPerfects[side] = perfects;
        finalMisses[side] = misses;
        finalMaxCombo[side] = maxCombo;

        playersReported++;

        if (playersReported >= 2)
        {
            ShowResultsClientRpc();
        }
    }

    [ClientRpc]
    private void ShowResultsClientRpc()
    {
        StartCoroutine(ShowResultsWithDelay());
    }

    private IEnumerator ShowResultsWithDelay()
    {
        yield return new WaitForSeconds(1.5f);

        resultsCanvas.SetActive(true);

        string left = $"LEFT PLAYER\n" +
                     $"Hits: {finalHits[0]}\n" +
                     $"Perfect: {finalPerfects[0]}\n" +
                     $"Misses: {finalMisses[0]}\n" +
                     $"Max Combo: {finalMaxCombo[0]}";

        string right = $"RIGHT PLAYER\n" +
                      $"Hits: {finalHits[1]}\n" +
                      $"Perfect: {finalPerfects[1]}\n" +
                      $"Misses: {finalMisses[1]}\n" +
                      $"Max Combo: {finalMaxCombo[1]}";

        if (leftResultsText) leftResultsText.text = left;
        if (rightResultsText) rightResultsText.text = right;
    }
}