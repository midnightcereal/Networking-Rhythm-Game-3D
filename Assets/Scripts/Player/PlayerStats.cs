using UnityEngine;
using Unity.Netcode;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Combo = new(0);
    public NetworkVariable<float> Health = new(100f);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Health.Value = 100f;
            Combo.Value = 0;
        }

        //Update UI when values change
        Combo.OnValueChanged += OnComboChanged;
        Health.OnValueChanged += OnHealthChanged;
    }

    private void OnComboChanged(int prev, int curr)
    {
        GameplayUI.Instance?.UpdatePlayer(OwnerClientId, curr, Health.Value);
    }

    private void OnHealthChanged(float prev, float curr)
    {
        GameplayUI.Instance?.UpdatePlayer(OwnerClientId, Combo.Value, curr);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateComboServerRpc(int newCombo)
    {
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