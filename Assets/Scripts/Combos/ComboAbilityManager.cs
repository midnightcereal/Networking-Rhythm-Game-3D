using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ComboAbilityManager : NetworkBehaviour
{
    public static ComboAbilityManager Instance { get; private set; }

    [Header("Ability Unlock Thresholds")]
    public int unlockPoint1 = 3;
    public int unlockPoint2 = 5;
    public int unlockPoint3 = 7;

    [Header("Ability Settings")]
    public float regenAmount = 30f;
    public float regenPerSecond = 5f;
    public float regenDuration = 5f;
    public float screenBlurDuration = 2f;
    public float hitlineHideDuration = 2.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    ///<summary>Called by local input to try use an ability, sends a ServerRpc to the host if not host</summary>
    public void TryUseAbility(int combo, ulong clientId)
    {
        int milestone = GetCurrentMilestone(combo);

        if (milestone == 0)
        {
            Debug.Log("[ComboAbility] No ability ready");
            return;
        }

        Debug.Log($"[ComboAbility] Milestone {milestone} reached! Pressed SPACE.");

        //Clear the local ability UI immediately
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            GameplayUI.Instance.ClearAbilityText();
        }

        //Execute ability via network
        if (NetworkManager.Singleton.IsHost)
        {
            //Host executes directly
            HostUseAbility(clientId, combo);
        }
        else
        {
            //Non host client requests host to execute
            RequestAbilityServerRpc(clientId, combo);
        }
    }

    ///<summary>ServerRpc called by client to request ability usage</summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestAbilityServerRpc(ulong clientId, int combo)
    {
        HostUseAbility(clientId, combo);
    }

    ///<summary>Host applies the ability effect and triggers ClientRpc for visuals</summary>
    private void HostUseAbility(ulong clientId, int combo)
    {
        int milestone = GetCurrentMilestone(combo);
        if (milestone == 0) return;

        var stats = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerStats>();
        if (stats == null) return;

        //Reset combo
        stats.Combo.Value = 0;

        float uiAnimationDuration = 0f;

        //Execute ability
        switch (milestone)
        {
            case 1: //Health regen self
                uiAnimationDuration = regenDuration;
                ApplyHealthRegenClientRpc(clientId, uiAnimationDuration);
                break;
            case 2: //Screen blur opponent
                uiAnimationDuration = screenBlurDuration;
                ulong opponentId = GetOpponentId(clientId);
                ApplyScreenBlurClientRpc(opponentId, uiAnimationDuration);
                break;
            case 3: //Hide hitline opponent
                uiAnimationDuration = 5f;
                ulong opponent2Id = GetOpponentId(clientId);
                ApplyHitlineHideClientRpc(opponent2Id, uiAnimationDuration);
                break;
        }

        GameplayUI.Instance.PulseComboOfPlayer(clientId, uiAnimationDuration);
    }

    private ulong GetOpponentId(ulong clientId)
    {
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            if (kvp.Key != clientId) return kvp.Key;
        return clientId; //fallback to self if single player
    }

    public int GetCurrentMilestone(int combo)
    {
        if (combo >= unlockPoint3) return 3;
        if (combo >= unlockPoint2) return 2;
        if (combo >= unlockPoint1) return 1;
        return 0;
    }

    #region ClientRpc Effects

    [ClientRpc]
    private void ApplyHealthRegenClientRpc(ulong targetClientId, float duration)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        StartCoroutine(HealthRegenCoroutine(duration));
    }

    [ClientRpc]
    private void ApplyScreenBlurClientRpc(ulong targetClientId, float duration)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        Debug.Log("[ComboAbility] Screen Blur triggered for client " + targetClientId);

        //Trigger the UI blur effect locally
        GameplayUI.Instance.TriggerScreenBlur(ComboAbilityManager.Instance.screenBlurDuration);
    }


    [ClientRpc]
    private void ApplyHitlineHideClientRpc(ulong targetClientId, float duration)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        //CALL HIDE HITLINE HERE
        Debug.Log("[ComboAbility] Hitline Hide triggered for client " + targetClientId);
    }

    #endregion

    #region Local Coroutines

    private IEnumerator HealthRegenCoroutine(float duration)
    {
        var stats = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerStats>();
        stats.Health.Value += regenAmount;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            stats.Health.Value = Mathf.Min(100f, stats.Health.Value + regenPerSecond * Time.deltaTime);
            yield return null;
        }
    }

    #endregion
}