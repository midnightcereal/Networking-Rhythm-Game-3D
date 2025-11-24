using UnityEngine;
using Unity.Netcode;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Combo = new(0);
    public NetworkVariable<float> Health = new(100f);

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerStats] OnNetworkSpawn - IsOwner: {IsOwner}, ClientId: {OwnerClientId}");

        if (IsOwner)
        {
            Health.Value = 100f;
            Combo.Value = 0;

            Debug.Log("[PlayerStats] Owner initialized: Health=100, Combo=0");
        }

        //Update UI when values change
        Combo.OnValueChanged += OnComboChanged;
        Health.OnValueChanged += OnHealthChanged;
    }

    private void OnComboChanged(int prev, int curr)
    {
        Debug.Log($"[PlayerStats] COMBO CHANGED: {prev} -> {curr} (Client {OwnerClientId})");
        GameplayUI.Instance?.UpdatePlayer(OwnerClientId, curr, Health.Value);
    }

    private void OnHealthChanged(float prev, float curr)
    {
        Debug.Log($"[PlayerStats] HEALTH CHANGED: {prev:F1} -> {curr:F1} (Client {OwnerClientId})");

        GameplayUI.Instance?.UpdatePlayer(OwnerClientId, Combo.Value, curr);

        //Permanent flash ONLY for local player for game over
        if (IsOwner && curr <= 0f)
        {
            MissEffect.Instance?.TriggerPermanentFlash();
            GameplayUI.Instance?.OnPlayerFailed(OwnerClientId);
            Debug.Log($"[PlayerStats] PLAYER {OwnerClientId} HAS FAILED - GAME OVER");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateComboServerRpc(int newCombo)
    {
        Debug.Log($"[PlayerStats] ServerRpc: UpdateComboServerRpc({newCombo}) from Client {OwnerClientId}");
        Combo.Value = newCombo;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeductHealthServerRpc(int previousCombo)
    {
        float deduction = 20f;
        if (previousCombo > 100) deduction = 5f;
        else if (previousCombo > 30) deduction = 10f;
        Health.Value = Mathf.Max(0f, Health.Value - deduction);
    }
}