using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : NetworkBehaviour
{
    public static GameplayUI Instance { get; private set; }

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

    ///<summary>Called from PlayerStats when values change</summary>
    public void UpdatePlayer(ulong clientId, int combo, float health)
    {
        Debug.Log($"[GameplayUI] UpdatePlayer called - ClientId: {clientId} | Combo: {combo} | Health: {health:F1}");

        int side = GetPlayerSide(clientId);

        Slider healthSlider = side == 0 ? leftHealthSlider : rightHealthSlider;
        Slider comboSlider = side == 0 ? leftComboSlider : rightComboSlider;

        comboSlider.value = combo;
        healthSlider.value = health;

        if (health <= 0f)
        {
            healthSlider.gameObject.SetActive(false);
        }
        else if (!healthSlider.gameObject.activeSelf)
        {
            healthSlider.gameObject.SetActive(true);
        }
    }

    private int GetPlayerSide(ulong clientId)
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