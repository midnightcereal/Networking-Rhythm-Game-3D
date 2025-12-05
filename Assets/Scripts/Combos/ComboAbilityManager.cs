using System.Collections;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ComboAbilityManager : NetworkBehaviour
{
    public static ComboAbilityManager Instance { get; private set; }

    [Header("Ability Unlock Values")]
    public int unlockPoint1 = 3;
    public int unlockPoint2 = 5;
    public int unlockPoint3 = 7;

    [Header("Local UI References - Assign in Inspector")]
    public TextMeshProUGUI localAbilityText;
    public GameObject localHitline;
    public Image localScreenBlurOverlay;

    [Header("Ability Settings")]
    public float screenBlurDuration = 2.0f;
    public float hitlineHideDuration = 2.5f;
    public float regenAmount = 30f;
    public float regenPerSecond = 5f;
    public float regenDuration = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    ///<summary>Called from GameplayUI when LOCAL combo updates</summary>
    public void CheckAbilityUnlock(int combo)
    {
        //if (!IsSpawned || !IsOwner) return;

        int milestone = GetCurrentMilestone(combo);
        Debug.Log("MILESTONE: " + milestone);
        if (milestone > 0 && localAbilityText != null)
        {
            Debug.Log("HIT MILESTONE");
            string abilityName = milestone switch
            {
                1 => "HEALTH REGEN BURST",
                2 => "SCREEN BLUR",
                3 => "HIDE HITLINE",
                _ => ""
            };
            localAbilityText.text = $"PRESS SPACE TO USE\n{abilityName}";
        }
        else if (localAbilityText != null)
        {
            localAbilityText.text = "";
        }
    }

    public int GetCurrentMilestone(int combo)
    {
        if (combo >= unlockPoint3) return 3;
        if (combo >= unlockPoint2) return 2;
        if (combo >= unlockPoint1) return 1;
        return 0;
    }

    //Called when press Space
    [ServerRpc(RequireOwnership = false)]
    public void UseAbilityServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var stats = NetworkManager.Singleton.ConnectedClients[clientId]
                    .PlayerObject?.GetComponent<PlayerStats>();
        if (stats == null) return;

        int milestone = GetCurrentMilestone(stats.Combo.Value);
        if (milestone == 0) return;

        //Reset combo
        stats.Combo.Value = 0;

        //Apply LOCAL effect to correct player
        switch (milestone)
        {
            case 1: //Health Regen (self)
                ApplyHealthRegenClientRpc(clientId);
                break;
            case 2: //Screen Blur
                ulong opponent = GetOpponentId(clientId);
                ApplyScreenBlurClientRpc(opponent);
                break;
            case 3: //Hide Hitline
                ulong opponent2 = GetOpponentId(clientId);
                ApplyHitlineHideClientRpc(opponent2);
                break;
        }
    }

    private ulong GetOpponentId(ulong clientId)
    {
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            if (kvp.Key != clientId) return kvp.Key;
        return clientId;
    }

    [ClientRpc]
    private void ApplyHealthRegenClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        StartCoroutine(HealthRegenCoroutine());
    }

    [ClientRpc]
    private void ApplyScreenBlurClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        StartCoroutine(ScreenBlurCoroutine());
    }

    [ClientRpc]
    private void ApplyHitlineHideClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        StartCoroutine(HitlineHideCoroutine());
    }

    private IEnumerator HealthRegenCoroutine()
    {
        var stats = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerStats>();
        stats.Health.Value += regenAmount;

        float elapsed = 0f;
        while (elapsed < regenDuration)
        {
            elapsed += Time.deltaTime;
            stats.Health.Value = Mathf.Min(100f, stats.Health.Value + regenPerSecond * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator ScreenBlurCoroutine()
    {
        if (localScreenBlurOverlay != null)
        {
            localScreenBlurOverlay.gameObject.SetActive(true);
            yield return new WaitForSeconds(screenBlurDuration);
            localScreenBlurOverlay.gameObject.SetActive(false);
        }
    }

    private IEnumerator HitlineHideCoroutine()
    {
        if (localHitline == null) yield break;

        localHitline.gameObject.SetActive(false);

        yield return new WaitForSeconds(hitlineHideDuration);

        localHitline.gameObject.SetActive(true);
    }
}