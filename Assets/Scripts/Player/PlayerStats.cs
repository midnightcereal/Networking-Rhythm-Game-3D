using UnityEngine;
using Unity.Netcode;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Combo = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Health = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> VisualCombo = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OnNetworkSpawn - IsOwner: {IsOwner}, ClientId: {OwnerClientId}");

        if (IsOwner)
        {
            Health.Value = 100f;
            Combo.Value = 0;

           // Debug.Log("Owner initialized: Health=100, Combo=0");
        }

        //Update UI when values change
        Combo.OnValueChanged += OnComboChanged;
        Health.OnValueChanged += OnHealthChanged;
        VisualCombo.OnValueChanged += (prev, curr) =>
        {
            GameplayUI.Instance?.UpdatePlayerVisualCombo(OwnerClientId, curr);
        };

        OnComboChanged(-1, Combo.Value);
        OnHealthChanged(-1f, Health.Value);
    }

    private void OnComboChanged(int prev, int curr)
    {
        //Debug.Log($"COMBO CHANGED: {prev} -> {curr} (Client {OwnerClientId})");
        GameplayUI.Instance?.UpdatePlayer(OwnerClientId, curr, Health.Value);
        //GameplayUI.Instance.ClearAbilityText();
    }

    private void OnHealthChanged(float prev, float curr)
    {
        //Debug.Log($"HEALTH CHANGED: {prev:F1} -> {curr:F1} (Client {OwnerClientId})");

        GameplayUI.Instance?.UpdatePlayer(OwnerClientId, Combo.Value, curr);

        //Permanent flash ONLY for local player for game over
        if (IsOwner && curr <= 0f)
        {
            MissEffect.Instance?.TriggerPermanentFlash();
            GameplayUI.Instance?.OnPlayerFailed(OwnerClientId);
            Debug.Log($"PLAYER {OwnerClientId} HAS FAILED - GAME OVER");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateComboServerRpc(int newCombo)
    {
        //Debug.Log($"ServerRpc: UpdateComboServerRpc({newCombo}) from Client {OwnerClientId}");
        Combo.Value = newCombo;
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncrementVisualComboServerRpc()
    {
        //Increase visual combo bar fill
        VisualCombo.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetVisualComboServerRpc(int newValue)
    {
        VisualCombo.Value = newValue;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeductHealthServerRpc(int previousCombo)
    {
        float deduction = 5f;
        if (previousCombo > 100) deduction = 1f;
        else if (previousCombo > 30) deduction = 2f;
        Health.Value = Mathf.Max(0f, Health.Value - deduction);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShowDamagePopupServerRpc(ulong clientId, float damageAmount)
    {
        ShowDamagePopupClientRpc(clientId, damageAmount);
    }

    [ClientRpc]
    public void ShowDamagePopupClientRpc(ulong clientId, float damageAmount)
    {
        //Call damage popup
        GameplayUI.Instance?.ShowDamagePopup(clientId, damageAmount);

        //Only trigger miss flash for the player who owns this client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            MissEffect.Instance?.TriggerMissFlash();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float healAmount)
    {
        //call health popup
        Health.Value = Mathf.Min(100f, Health.Value + healAmount);
        ShowHealthPopupClientRpc(OwnerClientId, healAmount);
    }

    [ClientRpc]
    public void ShowHealthPopupClientRpc(ulong clientId, float healAmount)
    {
        //Only show popup for local player
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            GameplayUI.Instance?.ShowHealthRegenPopup(clientId, healAmount);
        }
    }
}