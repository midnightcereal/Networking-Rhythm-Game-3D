using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    private const string COINS_KEY = "TotalPlayerCoins";

    private int currentCoins = 0;
    private bool coinsAwardedThisGame = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCoins();
    }

    private void LoadCoins()
    {
        currentCoins = PlayerPrefs.GetInt(COINS_KEY, 0);
        Debug.Log($"[CoinManager] Loaded {currentCoins} coins from PlayerPrefs");
    }

    public void SaveCoins()
    {
        PlayerPrefs.SetInt(COINS_KEY, currentCoins);
        PlayerPrefs.Save();
        Debug.Log($"[CoinManager] Saved {currentCoins} coins to PlayerPrefs");
    }

    ///<summary>Called at the end of a game</summary>
    public void AwardEndOfGameCoins(int localSide, int winnerSide,
        int hits, int perfects, int misses, int maxCombo)
    {
        if (coinsAwardedThisGame)
        {
            Debug.LogWarning("[CoinManager] Coins already awarded this game - ignoring duplicate call");
            return;
        }

        coinsAwardedThisGame = true;

        int score = (perfects * 100) + (hits * 10) + (maxCombo * 5) - (misses * 3);

        int coinsEarned = score;

        //Double coins if this player is the winner (tie = no double)
        bool isWinner = (winnerSide == localSide) && (winnerSide != -1);

        if (isWinner)
            coinsEarned *= 2;

        currentCoins += coinsEarned;

        Debug.Log($"[CoinManager] Awarded {coinsEarned} coins | Score: {score} | Winner: {isWinner} | Total: {currentCoins}");

        SaveCoins();
    }

    //Reset flag when a new game starts
    public void ResetForNewGame()
    {
        Debug.Log("[CoinManager] coinsAwarded flag reset");
        coinsAwardedThisGame = false;
    }

    //Calculates coins using the same scoring as DetermineWinner()
    public int CalculateCoins(int hits, int perfects, int misses, int maxCombo, bool isWinner)
    {
        int score = (perfects * 100) + (hits * 10) + (maxCombo * 5) - (misses * 3);
        int coins = score;
        if (isWinner) coins *= 2;
        return coins;
    }

    public void SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            SaveCoins();
            Debug.Log($"[CoinManager] Spent {amount} coins. Remaining: {currentCoins}");
        }
    }

    //Get current coins for UI in lobby
    public int GetCurrentCoins() => currentCoins;
}