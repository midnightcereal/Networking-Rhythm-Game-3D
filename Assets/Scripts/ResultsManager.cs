using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ResultsManager : NetworkBehaviour
{
    public static ResultsManager Instance;

    [Header("Results UI")]
    public GameObject resultsCanvas;
    public TextMeshProUGUI leftResultsText;
    public TextMeshProUGUI leftWinnerText;
    public TextMeshProUGUI rightResultsText;
    public TextMeshProUGUI rightWinnerText;
    public TextMeshProUGUI tieText;

    private AudioSource songAudioSource;
    private SongManager songManager;

    //Local stats
    private int localHits = 0;
    private int localPerfects = 0;
    private int localMisses = 0;
    private int localMaxCombo = 0;
    private int currentCombo = 0;

    //Final stats
    private Dictionary<int, (int hits, int perfects, int misses, int maxCombo)> playerStats = new Dictionary<int, (int, int, int, int)>();

    public int playersSubmitted = 0;
    public bool hasSubmitted = false;
    public bool hasShownResults = false;
    public bool hasSongEnded = false;

    [Header("FF Variables")]
    private bool opponentDisconnected = false;
    private int disconnectedWinnerSide = -1;

    [Header("Coin Display")]
    public TextMeshProUGUI leftCoinsText;
    public TextMeshProUGUI rightCoinsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        resultsCanvas.SetActive(false);
    }

    private void Start()
    {
        Debug.Log("[ResultsManager] IsSpawned = " + NetworkObject.IsSpawned);
        Debug.Log("[ResultsManager] IsOwner = " + IsOwner);
        Debug.Log("[ResultsManager] IsClient = " + IsClient);

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

        //ONLY HOST decides song end
        if (IsHost && remaining <= 0.2f && !hasSubmitted)
        {
            hasSubmitted = true;
            if (opponentDisconnected)
            {
                Debug.Log("[ResultsManager] Opponent disconnected -> showing forfeit win after song ends");
                BroadcastForfeitWinClientRpc(disconnectedWinnerSide);
            }
            else
            {
                Debug.Log("[ResultsManager] HOST ENDED SONG — COLLECTING STATS AND SHOWING RESULTS");
                //Host collects their own stats
                int mySide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);
                SubmitStatsLocally(mySide);
                //Retrieve clients stats
                AskClientsForStats();
            }
        }
    }

    private void SubmitStatsLocally(int side)
    {
        playerStats[side] = (localHits, localPerfects, localMisses, localMaxCombo);
    }

    private void AskClientsForStats()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId) continue;
            RequestClientStatsClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            });
        }
    }

    [ClientRpc]
    private void RequestClientStatsClientRpc(ClientRpcParams rpcParams = default)
    {
        Debug.Log("HOST ASKED FOR CLIENTRPC (client)");

        if (IsHost) return;

        if (!hasSubmitted)
        {
            Debug.Log("CLIENTRPC RECEIVED AND IS SUBMITTING STATS");
            hasSubmitted = true;

            int mySide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);

            SubmitClientStatsServerRpc(localHits, localPerfects, localMisses, localMaxCombo, mySide);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitClientStatsServerRpc(int hits, int perfects, int misses, int maxCombo, int side)
    {
        playerStats[side] = (hits, perfects, misses, maxCombo);

        //If stats received from all connected players, broadcast final results
        if (playerStats.Count == NetworkManager.Singleton.ConnectedClients.Count)
        {
            BroadcastFinalResultsClientRpc(
                playerStats.ContainsKey(0) ? playerStats[0].hits : 0,
                playerStats.ContainsKey(0) ? playerStats[0].perfects : 0,
                playerStats.ContainsKey(0) ? playerStats[0].misses : 0,
                playerStats.ContainsKey(0) ? playerStats[0].maxCombo : 0,
                playerStats.ContainsKey(1) ? playerStats[1].hits : 0,
                playerStats.ContainsKey(1) ? playerStats[1].perfects : 0,
                playerStats.ContainsKey(1) ? playerStats[1].misses : 0,
                playerStats.ContainsKey(1) ? playerStats[1].maxCombo : 0
            );
        }
    }

    [ClientRpc]
    private void BroadcastFinalResultsClientRpc(int p1Hits, int p1Perfects, int p1Misses, int p1MaxCombo, int p2Hits, int p2Perfects, int p2Misses, int p2MaxCombo)
    {
        this.playerStats[0] = (p1Hits, p1Perfects, p1Misses, p1MaxCombo);
        this.playerStats[1] = (p2Hits, p2Perfects, p2Misses, p2MaxCombo);

        StartCoroutine(ShowResultsAfterDelay());
    }

    private IEnumerator ShowResultsAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        resultsCanvas.SetActive(true);

        //Hide game over canvas
        if (MissEffect.Instance != null)
            MissEffect.Instance.HideGameOver();

        string leftName = GetPlayerNameForSide(0);
        string rightName = GetPlayerNameForSide(1);

        var left = playerStats.ContainsKey(0)
            ? $"{leftName}\nHits: {playerStats[0].hits}\nPerfect: {playerStats[0].perfects}\nMisses: {playerStats[0].misses}\nMax Combo: {playerStats[0].maxCombo}"
            : leftName;

        var right = playerStats.ContainsKey(1)
            ? $"{rightName}\nHits: {playerStats[1].hits}\nPerfect: {playerStats[1].perfects}\nMisses: {playerStats[1].misses}\nMax Combo: {playerStats[1].maxCombo}"
            : rightName;

        if (leftResultsText) leftResultsText.text = left;
        if (rightResultsText) rightResultsText.text = right;

        //Determine winner and add winner text + confetti
        int winnerSide = DetermineWinner();

        if (CoinManager.Instance != null)
        {
            bool leftIsWinner = winnerSide == 0;
            bool rightIsWinner = winnerSide == 1;

            (int hits0, int perfects0, int misses0, int maxCombo0) = playerStats.ContainsKey(0)
                ? playerStats[0]
                : (0, 0, 0, 0);

            (int hits1, int perfects1, int misses1, int maxCombo1) = playerStats.ContainsKey(1)
                ? playerStats[1]
                : (0, 0, 0, 0);

            int leftCoins = CoinManager.Instance.CalculateCoins(hits0, perfects0, misses0, maxCombo0, leftIsWinner);
            int rightCoins = CoinManager.Instance.CalculateCoins(hits1, perfects1, misses1, maxCombo1, rightIsWinner);

            leftCoinsText.gameObject.SetActive(true);
            rightCoinsText.gameObject.SetActive(true);

            if (leftCoinsText) leftCoinsText.text = $"+{leftCoins} coins";
            if (rightCoinsText) rightCoinsText.text = $"+{rightCoins} coins";

            //Award coins only to the local player
            int mySide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);
            var myStats = playerStats[mySide];
            CoinManager.Instance.AwardEndOfGameCoins(
                mySide,
                winnerSide,
                myStats.hits,
                myStats.perfects,
                myStats.misses,
                myStats.maxCombo);
        }

        //Force background to black when results appear
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }
    }

    private int DetermineWinner()
    {
        Debug.Log("Called determinewinner()");

        if (!playerStats.ContainsKey(0) || !playerStats.ContainsKey(1))
            return -1;

        Debug.Log("Made it to winner");

        var p0 = playerStats[0];
        var p1 = playerStats[1];

        //Score system
        int score0 = (p0.perfects * 100) + (p0.hits * 10) + (p0.maxCombo * 5) - (p0.misses * 3);
        int score1 = (p1.perfects * 100) + (p1.hits * 10) + (p1.maxCombo * 5) - (p1.misses * 3);

        if (score0 > score1)
        {
            leftWinnerText.gameObject.SetActive(true);
            Debug.Log("Left Winner");
            return 0;
        }
        else if (score1 > score0)
        {
            rightWinnerText.gameObject.SetActive(true);
            Debug.Log("Right Winner");
            return 1;
        }
        else
        {
            tieText.gameObject.SetActive(true);
            Debug.Log("no Winner");
            return -1; //tie
        }
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

    [ClientRpc]
    private void BroadcastForfeitWinClientRpc(int winnerSide)
    {
        ShowForfeitResults(winnerSide);
    }

    public void ShowForfeitResults(int winnerSide)
    {
        hasSubmitted = true; //prevent normal song end results

        string winnerName = GetPlayerNameForSide(winnerSide);
        string loserName = GetPlayerNameForSide(1 - winnerSide);

        string winnerText = $"{winnerName}\nHits: {localHits}\nPerfect: {localPerfects}\nMisses: {localMisses}\nMax Combo: {localMaxCombo}";

        string loserText = $"{loserName}\n(Disconnected)";

        if (winnerSide == 0)
        {
            leftResultsText.text = winnerText;
            rightResultsText.text = loserText;
            leftWinnerText.gameObject.SetActive(true);
            rightWinnerText.gameObject.SetActive(false);
            tieText.gameObject.SetActive(false);
        }
        else
        {
            rightResultsText.text = winnerText;
            leftResultsText.text = loserText;
            rightWinnerText.gameObject.SetActive(true);
            leftWinnerText.gameObject.SetActive(false);
            tieText.gameObject.SetActive(false);
        }

        //Force black background
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        //Stop song if still playing
        if (songAudioSource != null)
            songAudioSource.Stop();

        resultsCanvas.SetActive(true);

        //Hide game over canvas
        if (MissEffect.Instance != null)
            MissEffect.Instance.HideGameOver();

        //Award coins to local player
        if (CoinManager.Instance != null)
        {
            int mySide = GameplayUI.Instance.GetPlayerSide(NetworkManager.Singleton.LocalClientId);
            bool iAmWinner = mySide == winnerSide;

            int myCoins = CoinManager.Instance.CalculateCoins(localHits, localPerfects, localMisses, localMaxCombo, iAmWinner);

            leftCoinsText.gameObject.SetActive(true);
            rightCoinsText.gameObject.SetActive(true);

            //Show coins on winner side only (loser gets 0)
            if (winnerSide == 0)
            {
                if (leftCoinsText) leftCoinsText.text = $"+{myCoins} coins";
                if (rightCoinsText) rightCoinsText.text = "+0 coins";
            }
            else
            {
                if (rightCoinsText) rightCoinsText.text = $"+{myCoins} coins";
                if (leftCoinsText) leftCoinsText.text = "+0 coins";
            }

            //Award to local player
            CoinManager.Instance.AwardEndOfGameCoins(mySide, winnerSide,
                localHits, localPerfects, localMisses, localMaxCombo);
        }
    }

    public void SetOpponentDisconnected(int winnerSide)
    {
        opponentDisconnected = true;
        disconnectedWinnerSide = winnerSide;
    }

    public void RegisterHit(bool isPerfect)
    {
        //If player's game is over count the hit as a miss instead
        if (MissEffect.Instance != null && MissEffect.Instance.IsGameOver)
        {
            RegisterMiss();
            return;
        }

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