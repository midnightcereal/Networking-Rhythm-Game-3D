using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> DisplayName = new(
        new FixedString128Bytes(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        //Only the owner sets their display name
        if (IsOwner)
        {
            string name = IsHost ? "Host" : "Client";
            DisplayName.Value = new FixedString128Bytes(name);
        }

        //Subscribe to changes so UI updates automatically
        DisplayName.OnValueChanged += (oldValue, newValue) =>
        {
            LobbyManager.Instance?.RebuildLobbyUI();
        };

        //Add this player to the lobby manager list
        LobbyManager.Instance?.PlayerSpawned(this);

        //Update UI immediately
        LobbyManager.Instance?.RebuildLobbyUI();
    }

    public override void OnNetworkDespawn()
    {
        //Remove this player from the lobby manager list
        LobbyManager.Instance?.PlayerDespawned(this);

        //Update UI
        LobbyManager.Instance?.RebuildLobbyUI();
    }
}