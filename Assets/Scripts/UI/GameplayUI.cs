using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static PlayerStats;

public class GameplayUI : NetworkBehaviour
{
    public static GameplayUI Instance { get; private set; }

    [Header("Player Names")]
    public TMPro.TextMeshProUGUI leftPlayerNameText;
    public TMPro.TextMeshProUGUI rightPlayerNameText;
    public readonly Dictionary<ulong, string> basePlayerNames = new();

    private readonly Dictionary<ulong, int> comboBarValues = new();

    [Header("Ability Feedback")]
    public TextMeshProUGUI abilityText;

    [Header("Screen Blur Overlay")]
    public Image blurOverlay;

    [Header("Combo Bar Parent Transforms")]
    public Transform leftComboParent;
    public Transform rightComboParent;

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

    public Dictionary<ulong, int> playerSide = new();

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupSlider(leftComboSlider, 50, 0f);
        SetupSlider(leftHealthSlider, 100f, 100f);
        SetupSlider(rightComboSlider, 50, 0f);
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

    public int GetComboBarValue(ulong clientId)
    {
        if (!comboBarValues.ContainsKey(clientId))
            comboBarValues[clientId] = 0;
        return comboBarValues[clientId];
    }

    public void SetLocalAbilityReadyText(int comboBarValue)
    {
        int milestone = ComboAbilityManager.Instance.GetCurrentMilestone(comboBarValue);

        if (milestone <= 0)
        {
            ClearAbilityText();
            return;
        }

        string abilityName = milestone switch
        {
            1 => "HEALTH REGEN BURST",
            2 => "SCREEN BLUR",
            3 => "HIDE HITLINE",
            _ => ""
        };

        abilityText.text = $"PRESS SPACE TO USE\n{abilityName}";
    }

    public void ClearAbilityText()
    {
        abilityText.text = "";
    }

    ///<summary>Called from PlayerStats when values change</summary>
    public void UpdatePlayer(ulong clientId, int currentCombo, float health)
    {
        int side = GetPlayerSide(clientId);
        Slider healthSlider = side == 0 ? leftHealthSlider : rightHealthSlider;
        Slider comboSlider = side == 0 ? leftComboSlider : rightComboSlider;
        Image fill = side == 0 ? leftComboFill : rightComboFill;
        Image background = side == 0 ? leftComboBackground : rightComboBackground;

        if (!comboBarValues.ContainsKey(clientId))
            comboBarValues[clientId] = currentCombo;

        //Update combo bar slider with bar value
        comboSlider.value = comboBarValues[clientId];
        healthSlider.value = health;

        if (fill != null && background != null)
        {
            int milestone = ComboAbilityManager.Instance.GetCurrentMilestone(comboBarValues[clientId]);
            int lastMilestone = side == 0 ? lastMilestoneLeft : lastMilestoneRight;

            if (milestone != lastMilestone || (currentCombo < ComboAbilityManager.Instance.unlockPoint1 && lastMilestone >= ComboAbilityManager.Instance.unlockPoint1))
            {
                Color targetFill = milestone switch
                {
                    7 => purpleColor,
                    5 => indigoColor,
                    3 => blueColor,
                    _ => defaultColor
                };

                fill.color = targetFill;

                if (side == 0) lastMilestoneLeft = milestone;
                else lastMilestoneRight = milestone;

                //Notify LOCAL ability system
                if (clientId == NetworkManager.Singleton.LocalClientId)
                    SetLocalAbilityReadyText(comboBarValues[clientId]);
            }
        }
    }

    public void ResetComboBar(ulong clientId)
    {
        var playerStats = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.SetVisualComboServerRpc(0);

        PulseComboOfPlayer(clientId, 0.2f);
    }

    public void IncrementComboBar(ulong clientId)
    {
        var playerStats = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerStats>();
        if (playerStats != null)
            playerStats.IncrementVisualComboServerRpc();
    }

    public void UpdatePlayerVisualCombo(ulong clientId, int visualComboValue)
    {
        if (!comboBarValues.ContainsKey(clientId))
            comboBarValues[clientId] = 0;

        comboBarValues[clientId] = visualComboValue;

        int side = GetPlayerSide(clientId);
        Slider comboSlider = side == 0 ? leftComboSlider : rightComboSlider;
        if (comboSlider != null)
            comboSlider.value = visualComboValue;

        //Update ability text for local player
        if (clientId == NetworkManager.Singleton.LocalClientId)
            SetLocalAbilityReadyText(visualComboValue);
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

    #region UI Animations
    ///<summary>Called from ComboManager when health is deducted</summary>
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
            text.color = Color.red;
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

    //==== UI PULSING ====
    ///<summary>Called from ComboAbilityManager on ability activate</summary>
    public void PulseComboOfPlayer(ulong clientId, float duration)
    {
        int side = GetPlayerSide(clientId);

        Transform target = side == 0 ? leftComboParent : rightComboParent;

        Debug.Log("Triggered UI PULSE");
        TriggerComboPulse(target, duration);
    }

    public void TriggerComboPulse(Transform target, float duration)
    {
        if (target == null)
        {
            Debug.LogWarning("TriggerComboPulse called but no target transform was provided.");
            return;
        }

        Debug.Log("Started UI PULSE");
        StartCoroutine(PulseRotateRoutine(target, duration));
    }

    private IEnumerator PulseRotateRoutine(Transform target, float duration)
    {
        float timer = 0f;
        float pulseSpeed = 8f;
        float pulseAmount = 5f;

        Vector3 originalRotation = target.localEulerAngles;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float angle = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

            Vector3 rot = originalRotation;
            rot.y += angle;

            target.localEulerAngles = rot;

            yield return null;
        }

        target.localEulerAngles = new Vector3(0,0,0);
    }
    //===================
    #endregion

    #region ComboAbilityManager Stuff
    ///<summary>Called by ComboAbilityManager</summary>
    public void TriggerScreenBlur(float duration)
    {
        if (blurOverlay == null)
        {
            Debug.LogWarning("No screen blur overlay assigned in GameplayUI!");
            return;
        }

        //StopAllCoroutines();
        StartCoroutine(ScreenBlurRoutine(duration));
    }

    private IEnumerator ScreenBlurRoutine(float duration)
    {
        //Fade in to alpha 1
        yield return StartCoroutine(FadeBlur(0f, 1f, 0.35f));

        //Stay visible for the ability duration
        yield return new WaitForSeconds(duration);

        //Fade back to alpha 0
        yield return StartCoroutine(FadeBlur(1f, 0f, 0.35f));
    }

    private IEnumerator FadeBlur(float from, float to, float time)
    {
        float elapsed = 0f;

        Color colour = blurOverlay.color;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time;

            colour.a = Mathf.Lerp(from, to, t);
            blurOverlay.color = colour;

            yield return null;
        }

        colour.a = to;
        blurOverlay.color = colour;
    }
    #endregion
}