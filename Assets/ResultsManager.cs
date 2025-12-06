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

    //Local stats
    private int localHits = 0;
    private int localPerfects = 0;
    private int localMisses = 0;
    private int localMaxCombo = 0;
    private int currentCombo = 0;

    //Final stats (filled by server)
    public int p1Hits, p1Perfects, p1Misses, p1MaxCombo;
    public int p2Hits, p2Perfects, p2Misses, p2MaxCombo;

    public int playersSubmitted = 0;
    public bool hasSubmitted = false;
    public bool hasShownResults = false;
    public bool hasSongEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Force spawn the net object - this caused issues because it wasn't "spawning"
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                netObj.Spawn();
                Debug.Log("[ResultsManager] Auto spawned the network object");
            }
        }

        resultsCanvas.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(FindSongManagerRoutine());
    }

    private IEnumerator FindSongManagerRoutine()
    {
        while (songManager == null || songAudioSource == null)
        {
            songManager = FindObjectOfType<SongManager>();
            if (songManager != null && songManager.audioSource != null)
            {
                songAudioSource = songManager.audioSource;
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Update()
    {
        if (songAudioSource == null || songAudioSource.clip == null) return;

        float remaining = songAudioSource.clip.length - songAudioSource.time;

        //ONLY HOST decides when song ends CHANGE TO 0.2f
        if (IsHost && remaining <= 83f && !hasSubmitted)
        {
            hasSubmitted = true;
            Debug.Log("[ResultsManager] HOST ENDED SONG — COLLECTING STATS AND SHOWING RESULTS");

            //Host collects their own stats
            int mySide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);
            if (mySide == 0)
            {
                p1Hits = localHits;
                p1Perfects = localPerfects;
                p1Misses = localMisses;
                p1MaxCombo = localMaxCombo;
            }
            else
            {
                p2Hits = localHits;
                p2Perfects = localPerfects;
                p2Misses = localMisses;
                p2MaxCombo = localMaxCombo;
            }

            //Host asks client for stats
            RequestClientStatsClientRpc();

            //Host shows results immediately
            StartCoroutine(ShowResultsAfterDelay());
        }
    }

    [ClientRpc]
    private void RequestClientStatsClientRpc()
    {
        //THIS IS NOT BEING CALLED ON CLIENT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Debug.Log("HOST ASKED FOR CLIENT RPC");
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        if (IsHost) return; //Host already submitted
        Debug.Log("HOST ASKED FOR CLIENT RPC - PASSED HOST CHECK");

        if (!hasSubmitted)
        {
            Debug.Log("DELIVERED CLIENT RPC");
            hasSubmitted = true;
            int mySide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);
            SubmitClientStatsServerRpc(localHits, localPerfects, localMisses, localMaxCombo, mySide);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitClientStatsServerRpc(int hits, int perfects, int misses, int maxCombo, int side)
    {
        if (side == 0)
        {
            p1Hits = hits;
            p1Perfects = perfects;
            p1Misses = misses;
            p1MaxCombo = maxCombo;
        }
        else
        {
            p2Hits = hits;
            p2Perfects = perfects;
            p2Misses = misses;
            p2MaxCombo = maxCombo;
        }
    }

    private IEnumerator ShowResultsAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        resultsCanvas.SetActive(true);

        string leftName = GetPlayerNameForSide(0);
        string rightName = GetPlayerNameForSide(1);

        string left = $"{leftName}\nHits: {p1Hits}\nPerfect: {p1Perfects}\nMisses: {p1Misses}\nMax Combo: {p1MaxCombo}";
        string right = $"{rightName}\nHits: {p2Hits}\nPerfect: {p2Perfects}\nMisses: {p2Misses}\nMax Combo: {p2MaxCombo}";

        if (leftResultsText) leftResultsText.text = left;
        if (rightResultsText) rightResultsText.text = right;
    }

    private string GetPlayerNameForSide(int side)
    {
        if (GameplayUI.Instance == null) return "PLAYER";

        foreach (var kvp in GameplayUI.Instance.playerSide)
        {
            if (kvp.Value == side)
            {
                string baseName = "PLAYER";
                if (GameplayUI.Instance.basePlayerNames.TryGetValue(kvp.Key, out string name))
                    baseName = name;

                bool isLocal = kvp.Key == NetworkManager.Singleton.LocalClientId;
                return isLocal ? $"{baseName} (You)" : baseName;
            }
        }
        return "PLAYER";
    }

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

    public void OnComboBreak() => currentCombo = 0;
}