using UnityEngine;

public class AbilityUpgradeManager : MonoBehaviour
{
    public static AbilityUpgradeManager Instance { get; private set; }

    //Upgrade levels (0 = not upgraded, 1-3 = levels)
    private int healthRegenLevel = 0;
    private int screenBlurLevel = 0;
    private int hideHitlineLevel = 0;

    //PlayerPrefs keys
    private const string KEY_REGEN = "Upgrade_HealthRegen";
    private const string KEY_BLUR = "Upgrade_ScreenBlur";
    private const string KEY_HITLINE = "Upgrade_HideHitline";

    //Upgrade costs (Level 1, 2, 3)
    public int[] regenCosts = { 80000, 160000, 320000 };
    public int[] blurCosts = { 60000, 130000, 280000 };
    public int[] hitlineCosts = { 120000, 240000, 500000 };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadUpgrades();
    }

    private void LoadUpgrades()
    {
        healthRegenLevel = PlayerPrefs.GetInt(KEY_REGEN, 0);
        screenBlurLevel = PlayerPrefs.GetInt(KEY_BLUR, 0);
        hideHitlineLevel = PlayerPrefs.GetInt(KEY_HITLINE, 0);
    }

    private void SaveUpgrades()
    {
        PlayerPrefs.SetInt(KEY_REGEN, healthRegenLevel);
        PlayerPrefs.SetInt(KEY_BLUR, screenBlurLevel);
        PlayerPrefs.SetInt(KEY_HITLINE, hideHitlineLevel);
        PlayerPrefs.Save();
    }

    //UPGRADE LOGIC
    public bool CanUpgrade(int abilityIndex) //0=Regen, 1=Blur, 2=Hitline
    {
        int currentLevel = GetCurrentLevel(abilityIndex);
        if (currentLevel >= 3) return false;

        int cost = GetCost(abilityIndex, currentLevel + 1);
        return CoinManager.Instance.GetCurrentCoins() >= cost;
    }

    public void UpgradeAbility(int abilityIndex)
    {
        if (!CanUpgrade(abilityIndex)) return;

        int currentLevel = GetCurrentLevel(abilityIndex);
        int cost = GetCost(abilityIndex, currentLevel + 1);

        //Deduct coins
        CoinManager.Instance.SpendCoins(cost);

        //Increase level
        SetLevel(abilityIndex, currentLevel + 1);
        SaveUpgrades();

        Debug.Log($"[AbilityUpgrade] Upgraded ability {abilityIndex} to level {currentLevel + 1}");
    }

    private int GetCurrentLevel(int abilityIndex)
    {
        return abilityIndex switch
        {
            0 => healthRegenLevel,
            1 => screenBlurLevel,
            2 => hideHitlineLevel,
            _ => 0
        };
    }

    private void SetLevel(int abilityIndex, int newLevel)
    {
        switch (abilityIndex)
        {
            case 0: healthRegenLevel = newLevel; break;
            case 1: screenBlurLevel = newLevel; break;
            case 2: hideHitlineLevel = newLevel; break;
        }
    }

    private int GetCost(int abilityIndex, int nextLevel)
    {
        int[] costs = abilityIndex switch
        {
            0 => regenCosts,
            1 => blurCosts,
            2 => hitlineCosts,
            _ => new int[0]
        };
        return (nextLevel >= 1 && nextLevel <= 3) ? costs[nextLevel - 1] : 0;
    }

    //UI DISPLAY
    ///<summary>Called from lobby upgrade buttons to show next effect</summary>
    public string GetUpgradeDescription(int abilityIndex)
    {
        int currentLevel = GetCurrentLevel(abilityIndex);
        if (currentLevel >= 3) return "MAX LEVEL";

        int nextLevel = currentLevel + 1;
        int cost = GetCost(abilityIndex, nextLevel);

        string effect = abilityIndex switch
        {
            0 => GetRegenDescription(nextLevel),
            1 => $"Duration: {GetScreenBlurDuration()}s",
            2 => $"Duration: {GetHideHitlineDuration()}s",
            _ => ""
        };

        return $"{effect}  ({cost} coins)";
    }

    private string GetRegenDescription(int nextLevel)
    {
        return nextLevel switch
        {
            1 => "Longer Regen (+2s)",
            2 => "Stronger Regen (+5 hp/s)",
            3 => "Longer + Stronger",
            _ => ""
        };
    }

    //EFFECT VALUES (used by ComboAbilityManager)
    public float GetRegenDuration() => 5f + (healthRegenLevel * 1.5f);
    public float GetRegenPerSecond() => 6f + (healthRegenLevel * 2f);

    public float GetScreenBlurDuration() => 2f + (screenBlurLevel * 1.5f);
    public float GetHideHitlineDuration() => 2.5f + (hideHitlineLevel * 2f);
}