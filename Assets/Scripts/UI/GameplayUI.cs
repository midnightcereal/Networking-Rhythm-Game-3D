using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UI;
using static PlayerStats;

public class GameplayUI : NetworkBehaviour
{
    public static GameplayUI Instance { get; private set; }

    [Header("Player Names")]
    public TMPro.TextMeshProUGUI leftPlayerNameText;
    public TMPro.TextMeshProUGUI rightPlayerNameText;
    private readonly Dictionary<ulong, string> basePlayerNames = new();

    [Header("Ability Feedback")]
    public TextMeshProUGUI leftAbilityText;
    public TextMeshProUGUI rightAbilityText;

    private string[] abilityNames = new string[]
    {
    "",
    "HEALTH REGEN BURST",
    "SCREEN BLUR",
    "HIDE HITLINE"
    };

    [Header("Player 1 - Left Side")]
    public Slider leftComboSlider;
    public Slider leftHealthSlider;

    [Header("Player 2 - Right Side")]
    public Slider rightComboSlider;
    public Slider rightHealthSlider;

    [Header("Damage Popup")]
    public GameObject damageTextPrefab;
    public Transform leftDamagePopupParent;
    public Transform rightDamagePopupParent;

    [Header("Combo Bar Colors")]
    public Image leftComboFill;
    public Image rightComboFill;
    public Image leftComboBackground;
    public Image rightComboBackground;

    private readonly Color defaultColor = Color.white;
    private readonly Color blueColor = new Color(0.0f, 0.0f, 0.7f);     //30+
    private readonly Color indigoColor = new Color(0.2f, 0.0f, 0.9f);     //50+
    private readonly Color purpleColor = new Color(0.7f, 0f, 1f);      //70+

    private int lastMilestoneLeft = 0;
    private int lastMilestoneRight = 0;

    private Dictionary<ulong, int> playerSide = new();

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupSlider(leftComboSlider, 100f, 0f);
        SetupSlider(leftHealthSlider, 100f, 100f);
        SetupSlider(rightComboSlider, 100f, 0f);
        SetupSlider(rightHealthSlider, 100f, 100f);

        if (leftPlayerNameText) leftPlayerNameText.text = "";
        if (rightPlayerNameText) rightPlayerNameText.text = "";

        StartCoroutine(ApplyPlayerNamesWhenReady());
    }

    private IEnumerator ApplyPlayerNamesWhenReady()
    {
        yield return null;

        var networkPlayers = FindObjectsOfType<NetworkPlayer>();

        foreach (var player in networkPlayers)
        {
            if (player.DisplayName.Value.Length > 0)
            {
                string baseName = player.DisplayName.Value.ToString();
                ulong clientId = player.OwnerClientId;

                int side = GetPlayerSide(clientId);
                bool isLocalPlayer = clientId == NetworkManager.Singleton.LocalClientId;

                string displayName = isLocalPlayer ? $"{baseName} (You)" : baseName;

                if (side == 0 && leftPlayerNameText)
                    leftPlayerNameText.text = displayName;
                else if (side == 1 && rightPlayerNameText)
                    rightPlayerNameText.text = displayName;
            }
        }
    }

    private void SetupSlider(Slider slider, float maxValue, float startValue)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxValue;
            slider.value = startValue;
            slider.interactable = false;
        }
    }

    public void SetAbilityReadyText(int side, string text)
    {
        if (side == 0 && leftAbilityText)
            leftAbilityText.text = text;
        else if (side == 1 && rightAbilityText)
            rightAbilityText.text = text;
    }

    public void ClearAbilityText(int side)
    {
        if (side == 0 && leftAbilityText)
            leftAbilityText.text = "";
        else if (side == 1 && rightAbilityText)
            rightAbilityText.text = "";
    }

    ///<summary>Called from PlayerStats when values change</summary>
    public void UpdatePlayer(ulong clientId, int combo, float health)
    {
        int side = GetPlayerSide(clientId);
        Slider healthSlider = side == 0 ? leftHealthSlider : rightHealthSlider;
        Slider comboSlider = side == 0 ? leftComboSlider : rightComboSlider;
        Image fill = side == 0 ? leftComboFill : rightComboFill;
        Image background = side == 0 ? leftComboBackground : rightComboBackground;

        //Update values
        comboSlider.value = combo;
        healthSlider.value = health;

        var ability = ComboAbilityManager.Instance;

        if (health <= 0f)
            healthSlider.gameObject.SetActive(false);
        else if (!healthSlider.gameObject.activeSelf)
            healthSlider.gameObject.SetActive(true);

        if (fill != null && background != null)
        {
            int currentMilestone = 0;
            if (combo >= ability.unlockPoint3) currentMilestone = ability.unlockPoint3;
            else if (combo >= ability.unlockPoint2) currentMilestone = ability.unlockPoint2;
            else if (combo >= ability.unlockPoint1) currentMilestone = ability.unlockPoint1;

            int lastMilestone = side == 0 ? lastMilestoneLeft : lastMilestoneRight;

            if (currentMilestone != lastMilestone || (combo < ability.unlockPoint1 && lastMilestone >= ability.unlockPoint1))
            {
                Color targetFill = currentMilestone switch
                {
                    7 => purpleColor,
                    5 => indigoColor,
                    3 => blueColor,
                    _ => defaultColor
                };

                fill.color = targetFill;

                if (side == 0) lastMilestoneLeft = currentMilestone;
                else lastMilestoneRight = currentMilestone;

                //Notify LOCAL ability system
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    ComboAbilityManager.Instance.CheckAbilityUnlock(combo);
                }
            }
        }
    }

    public int GetPlayerSide(ulong clientId)
    {
        if (!playerSide.ContainsKey(clientId))
        {
            //First player = left, second = right
            int index = playerSide.Count;
            playerSide[clientId] = index;
        }
        return playerSide[clientId];
    }

    public void OnPlayerFailed(ulong clientId)
    {
        int side = GetPlayerSide(clientId);
        Slider healthSlider = side == 0 ? leftHealthSlider : rightHealthSlider;

        //Permanently disable health bar
        healthSlider.gameObject.SetActive(false);
    }

    ///<summary>
    ///Called from ComboManager when health is deducted
    ///</summary>
    public void ShowDamagePopup(ulong clientId, float damageAmount)
    {
        if (damageTextPrefab == null) return;

        int side = GetPlayerSide(clientId);
        Transform parent = side == 0 ? leftDamagePopupParent : rightDamagePopupParent;
        if (parent == null) return;

        GameObject popupObj = Instantiate(damageTextPrefab, parent);
        TextMeshProUGUI text = popupObj.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"-{damageAmount:F0}";
            text.color = damageAmount >= 10f ? Color.red : new Color(1f, 0.5f, 0f); // Orange for 5
            StartCoroutine(AnimateDamagePopup(popupObj));
        }
    }

    private IEnumerator AnimateDamagePopup(GameObject popup)
    {
        RectTransform rt = popup.GetComponent<RectTransform>();
        Vector3 startPos = Vector3.zero;
        Vector3 endPos = Vector3.up * 50f;

        float duration = 0.6f;
        float timer = 0f;

        //Pop out + fade in
        while (timer < duration * 0.5f)
        {
            timer += Time.deltaTime;
            float t = timer / (duration * 0.5f);
            rt.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            popup.GetComponent<TextMeshProUGUI>().alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        //Fade out
        timer = 0f;
        while (timer < duration * 0.5f)
        {
            timer += Time.deltaTime;
            float t = timer / (duration * 0.5f);
            popup.GetComponent<TextMeshProUGUI>().alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        Destroy(popup);
    }
}